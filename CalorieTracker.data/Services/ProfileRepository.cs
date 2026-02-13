using System.Collections.Concurrent;
using System.ComponentModel;
using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CalorieTracker.data.Models.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class ProfileRepository : IProfileRepository, IDisposable, INotifyPropertyChanged
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IWeightService _weightService;
        private readonly ILogger<ProfileRepository> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDbContextFactory _dbContextFactory;

        private UserProfile? _currentProfile;
        private UserSettings? _currentSettings;
        private List<WeightLog> _weightLogs = [];

        // Undo/redo stacks
        private readonly Stack<UserProfile> _undoStack = new();
        private readonly Stack<UserProfile> _redoStack = new();

        private readonly Stack<UserSettings> _settingsUndoStack = new();
        private readonly Stack<UserSettings> _settingsRedoStack = new();

        // save 
        private CancellationTokenSource? _profileSaveCts;
        private CancellationTokenSource? _settingsSaveCts;
        private readonly TimeSpan _autoSaveDelay = TimeSpan.FromSeconds(1.5);
        private readonly ConcurrentQueue<Func<Task>> _pendingProfileSaves = new();
        private readonly ConcurrentQueue<Func<Task>> _pendingSettingsSaves = new();
        private readonly object _saveLock = new object();
        private bool _isDisposed;

        // Status
        private bool _hasUnsavedChanges;
        private bool _isSaving;
        private bool _isInitialized;
        private bool _isProcessingExternalUpdate;

        public UserProfile? CurrentProfile => _currentProfile;
        public UserSettings? CurrentSettings => _currentSettings;
        public List<WeightLog> WeightLogs => _weightLogs.ToList();

        public bool HasUnsavedChanges => _hasUnsavedChanges;
        public bool IsSaving => _isSaving;
        public bool IsInitialized => _isInitialized;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ProfileRepository(
            IUserProfileService userProfileService,
            IWeightService weightService,
            ILogger<ProfileRepository> logger,
            IEventAggregator eventAggregator,
            IDbContextFactory dbContextFactory)
        {
            _userProfileService = userProfileService;
            _weightService = weightService;
            _logger = logger;
            _eventAggregator = eventAggregator;
            _dbContextFactory = dbContextFactory;

            _eventAggregator.Subscribe<ProfileUpdatedEvent>(OnProfileUpdatedExternal);
            _eventAggregator.Subscribe<SettingsUpdatedEvent>(OnSettingsUpdatedExternal);
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("ProfileRepository: Starting initialization...");

            try
            {
                if (_isInitialized) return;

                _logger.LogInformation("ProfileRepository: Semaphore acquired, proceeding...");

                _currentProfile = await _userProfileService.GetUserProfileAsync();
                _currentSettings = await _userProfileService.GetUserSettingsAsync();
                _weightLogs = await _weightService.GetWeightLogsAsync(365);

                // Clear undo history on initial load
                _undoStack.Clear();
                _redoStack.Clear();
                _settingsUndoStack.Clear();
                _settingsRedoStack.Clear();

                _isInitialized = true;
                _hasUnsavedChanges = false;

                // Notify UI
                OnPropertyChanged(nameof(CurrentProfile));
                OnPropertyChanged(nameof(CurrentSettings));
                OnPropertyChanged(nameof(WeightLogs));
                OnPropertyChanged(nameof(HasUnsavedChanges));
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));

                // PUBLISH VIA EVENT AGGREGATOR:
                await _eventAggregator.PublishAsync(new ProfileUpdatedEvent(_currentProfile, true));
                await _eventAggregator.PublishAsync(new SettingsUpdatedEvent(_currentSettings, true));
                await _eventAggregator.PublishAsync(new WeightLogsChangedEvent(WeightLogs));

                _logger.LogInformation("ProfileRepository initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ProfileRepository");

                // PUBLISH SYSTEM ERROR:
                await _eventAggregator.PublishAsync(new SystemErrorEvent(
                    "Failed to initialize profile repository",
                    ex,
                    nameof(ProfileRepository),
                    ErrorSeverity.Critical
                ));

                throw;
            }
        }

        public async Task UpdateProfileAsync(Action<UserProfile> updateAction)
        {
            if (_currentProfile == null)
                throw new InvalidOperationException("Repository not initialized");

            // Save to undo stack before changes
            _undoStack.Push(CloneProfile(_currentProfile));
            _redoStack.Clear();

            // Apply changes
            updateAction(_currentProfile);
            _currentProfile.LastUpdatedDate = DateTime.UtcNow;

            // Mark as dirty
            _hasUnsavedChanges = true;

            // PUBLISH VIA EVENT AGGREGATOR:
            await _eventAggregator.PublishAsync(new ProfileUpdatedEvent(_currentProfile));
            OnPropertyChanged(nameof(CurrentProfile)); 
            OnPropertyChanged(nameof(HasUnsavedChanges));

            // Schedule auto-save
            await ScheduleProfileAutoSaveAsync(async () =>
            {
                try
                {
                    await _userProfileService.UpdateUserProfileAsync(_currentProfile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-save failed for profile update");

                    // PUBLISH SYSTEM ERROR:
                    await _eventAggregator.PublishAsync(new SystemErrorEvent(
                        "Failed to auto-save profile",
                        ex,
                        nameof(UpdateProfileAsync),
                        ErrorSeverity.Error
                    ));
                }
            });
        }

        public async Task UpdateSettingsAsync(Action<UserSettings> updateAction)
        {
            if (_currentSettings == null)
                throw new InvalidOperationException("Repository not initialized");

            // Save to undo stack before changes
            _settingsUndoStack.Push(CloneSettings(_currentSettings!));
            _settingsRedoStack.Clear();

            // Apply changes
            updateAction(_currentSettings);
            _currentSettings.LastUpdatedDate = DateTime.UtcNow;

            // Mark as dirty
            _hasUnsavedChanges = true;

            // PUBLISH VIA EVENT AGGREGATOR:
            await _eventAggregator.PublishAsync(new SettingsUpdatedEvent(_currentSettings));
            OnPropertyChanged(nameof(CurrentSettings));
            OnPropertyChanged(nameof(HasUnsavedChanges));

            // Schedule auto-save
            await ScheduleSettingsAutoSaveAsync(async () =>
            {
                try
                {
                    await _userProfileService.UpdateUserSettingsAsync(_currentSettings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-save failed for settings update");

                    // PUBLISH SYSTEM ERROR:
                    await _eventAggregator.PublishAsync(new SystemErrorEvent(
                        "Failed to auto-save settings",
                        ex,
                        nameof(UpdateSettingsAsync),
                        ErrorSeverity.Error
                    ));
                }
            });
        }

        public async Task<WeightLog> AddWeightLogAsync(WeightLog weightLog)
        {
            var savedLog = await _weightService.AddWeightLogAsync(weightLog);
            _weightLogs.Add(savedLog);

            // Sort by date
            _weightLogs = _weightLogs.OrderBy(w => w.LogDate).ToList();

            // PUBLISH VIA EVENT AGGREGATOR:
            await _eventAggregator.PublishAsync(new WeightLogsChangedEvent(
                WeightLogs,
                addedOrUpdated: savedLog
            ));

            return savedLog;
        }

        public async Task<bool> UpdateWeightLogAsync(WeightLog weightLog)
        {
            var success = await _weightService.UpdateWeightLogAsync(weightLog);

            if (success)
            {
                var index = _weightLogs.FindIndex(w => w.Id == weightLog.Id);
                if (index >= 0)
                {
                    _weightLogs[index] = weightLog;
                    // PUBLISH VIA EVENT AGGREGATOR:
                    await _eventAggregator.PublishAsync(new WeightLogsChangedEvent(
                        WeightLogs,
                        addedOrUpdated: weightLog
                    ));
                }
            }

            return success;
        }

        public async Task<bool> DeleteWeightLogAsync(int id)
        {
            var success = await _weightService.DeleteWeightLogAsync(id);

            if (success)
            {
                _weightLogs.RemoveAll(w => w.Id == id);
                // PUBLISH VIA EVENT AGGREGATOR:
                await _eventAggregator.PublishAsync(new WeightLogsChangedEvent(
                    WeightLogs,
                    deletedId: id
                ));
            }

            return success;
        }

        public async Task SaveChangesAsync()
        {
            if (!_hasUnsavedChanges) return;

            _isSaving = true;

            try
            {
                // Save all pending changes
                await ExecutePendingSavesAsync(_pendingProfileSaves, "profile");
                await ExecutePendingSavesAsync(_pendingSettingsSaves, "settings");

                _hasUnsavedChanges = false;
                // PUBLISH VIA EVENT AGGREGATOR
                await _eventAggregator.PublishAsync(new SaveCompletedEvent(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual save failed");

                // PUBLISH VIA EVENT AGGREGATOR:
                await _eventAggregator.PublishAsync(new SaveCompletedEvent(false, ex));

                // ALSO PUBLISH SYSTEM ERROR:
                await _eventAggregator.PublishAsync(new SystemErrorEvent(
                    "Manual save failed",
                    ex,
                    nameof(SaveChangesAsync),
                    ErrorSeverity.Error
                ));

                throw;
            }
            finally
            {
                _isSaving = false;
            }
        }

        public async Task UndoAsync()
        {
            if (!CanUndo || _currentProfile == null || _currentSettings == null) return;

            // Profile undo
            _redoStack.Push(CloneProfile(_currentProfile));
            _currentProfile = _undoStack.Pop();

            // Settings undo (if any)
            bool settingsChanged = false;
            if (_settingsUndoStack.Count > 0)
            {
                _settingsRedoStack.Push(CloneSettings(_currentSettings));
                _currentSettings = _settingsUndoStack.Pop();
                settingsChanged = true;
            }

            _hasUnsavedChanges = true;
            await _eventAggregator.PublishAsync(new ProfileUpdatedEvent(_currentProfile));
            if (settingsChanged)
            {
                await _eventAggregator.PublishAsync(new SettingsUpdatedEvent(_currentSettings));
            }
            OnPropertyChanged(nameof(CurrentProfile));
            OnPropertyChanged(nameof(CurrentSettings)); // if changed
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));

            // Save what actually changed
            await ScheduleProfileAutoSaveAsync(async () =>
            {
                try
                {
                    await _userProfileService.UpdateUserProfileAsync(_currentProfile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Profile save failed after undo");
                }
            });

            if (settingsChanged)
            {
                await ScheduleSettingsAutoSaveAsync(async () =>
                {
                    try
                    {
                        await _userProfileService.UpdateUserSettingsAsync(_currentSettings);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Settings save failed after undo");
                    }
                });
            }
        }

        public async Task RedoAsync()
        {
            // Mirror the same logic as UndoAsync but for redo stacks
            if (!CanRedo || _currentProfile == null || _currentSettings == null) return;

            // Current state goes to undo stack
            _undoStack.Push(CloneProfile(_currentProfile));

            // Restore from redo stack
            _currentProfile = _redoStack.Pop();

            bool settingsChanged = false;
            // Add undo for settings if stack has entries
            if (_settingsRedoStack.Count > 0)
            {
                _settingsRedoStack.Push(CloneSettings(_currentSettings));
                _currentSettings = _settingsRedoStack.Pop();
                settingsChanged = true;
            }

            // Mark as dirty
            _hasUnsavedChanges = true;

            // Notify property changes
            OnPropertyChanged(nameof(CurrentProfile));
            if (settingsChanged)
            {
                OnPropertyChanged(nameof(CurrentSettings));
            }
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));

            await _eventAggregator.PublishAsync(new ProfileUpdatedEvent(_currentProfile));
            if (settingsChanged)
            {
                await _eventAggregator.PublishAsync(new SettingsUpdatedEvent(_currentSettings));
            }

            // Save the redo state
            // Save what actually changed
            await ScheduleProfileAutoSaveAsync(async () =>
            {
                try
                {
                    await _userProfileService.UpdateUserProfileAsync(_currentProfile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Profile save failed after redo");
                }
            });

            if (settingsChanged)
            {
                await ScheduleSettingsAutoSaveAsync(async () =>
                {
                    try
                    {
                        await _userProfileService.UpdateUserSettingsAsync(_currentSettings);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Settings save failed after redo");
                    }
                });
            }
        }

        private async Task ScheduleProfileAutoSaveAsync(Func<Task> saveAction)
        {
            _profileSaveCts?.Cancel();
            _profileSaveCts = new CancellationTokenSource();

            _pendingProfileSaves.Enqueue(saveAction);

            var token = _profileSaveCts.Token;

            try
            {
                await Task.Delay(_autoSaveDelay, token);

                if (!token.IsCancellationRequested)
                {
                    await ExecutePendingSavesAsync(_pendingProfileSaves, "profile");
                }
            }
            catch (TaskCanceledException)
            {
                // New change came in → expected
            }
        }

        private async Task ScheduleSettingsAutoSaveAsync(Func<Task> saveAction)
        {
            _settingsSaveCts?.Cancel();
            _settingsSaveCts = new CancellationTokenSource();

            _pendingSettingsSaves.Enqueue(saveAction);

            var token = _settingsSaveCts.Token;

            try
            {
                await Task.Delay(_autoSaveDelay, token);

                if (!token.IsCancellationRequested)
                {
                    await ExecutePendingSavesAsync(_pendingSettingsSaves, "settings");
                }
            }
            catch (TaskCanceledException)
            {
                // Expected
            }
        }

        private async Task ExecutePendingSavesAsync(ConcurrentQueue<Func<Task>> queue, string type)
        {
            lock (_saveLock)
            {
                if (_isSaving) return;
                _isSaving = true;
            }

            try
            {
                while (queue.TryDequeue(out var save))
                {
                    try
                    {
                        await save();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Auto-save failed for {Type}", type);
                        await _eventAggregator.PublishAsync(new SystemErrorEvent(
                            $"Failed to auto-save {type}",
                            ex,
                            nameof(ExecutePendingSavesAsync),
                            ErrorSeverity.Error));
                    }
                }

                if (!queue.IsEmpty) return; // Still more? (unlikely)

                _hasUnsavedChanges = false;
                await _eventAggregator.PublishAsync(new SaveCompletedEvent(true));
                OnPropertyChanged(nameof(HasUnsavedChanges));
                OnPropertyChanged(nameof(IsSaving));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical auto-save failure for {Type}", type);
                await _eventAggregator.PublishAsync(new SaveCompletedEvent(false, ex));
            }
            finally
            {
                _isSaving = false;
            }
        }

        private UserProfile CloneProfile(UserProfile profile)
        {
            return new UserProfile
            {
                Id = profile.Id,
                Name = profile.Name,
                BirthDate = profile.BirthDate,
                Gender = profile.Gender,
                HeightCm = profile.HeightCm,
                ActivityLevel = profile.ActivityLevel,
                WeightGoal = profile.WeightGoal,
                WeightChangeRateKgPerWeek = profile.WeightChangeRateKgPerWeek,
                CreatedDate = profile.CreatedDate,
                LastUpdatedDate = profile.LastUpdatedDate,
                HasCompletedWizard = profile.HasCompletedWizard,
                WizardCompletedDate = profile.WizardCompletedDate
            };
        }

        private UserSettings CloneSettings(UserSettings settings)
        {
            return new UserSettings
            {
                Id = settings.Id,
                ProteinPercentage = settings.ProteinPercentage,
                CarbsPercentage = settings.CarbsPercentage,
                FatPercentage = settings.FatPercentage,
                UseMetricSystem = settings.UseMetricSystem,
                Theme = settings.Theme,
                TrackMacros = settings.TrackMacros,
                TrackWater = settings.TrackWater,
                DietType = settings.DietType,
                MealReminderTime = settings.MealReminderTime,
                EnableMealReminders = settings.EnableMealReminders,
                CreatedDate = settings.CreatedDate,
                LastUpdatedDate = settings.LastUpdatedDate
            };
        }

        private void OnProfileUpdatedExternal(ProfileUpdatedEvent e)
        {
            if (e.IsFromInitialization) return;

            if (_isProcessingExternalUpdate) return;

            try
            {
                _isProcessingExternalUpdate = true;

                _logger.LogInformation("ProfileRepository: Received ProfileUpdatedEvent for profile {Id}",
                    e.Profile?.Id);

                if (e.Profile != null && e.Profile.Id == (_currentProfile?.Id ?? 1))
                {
                    _currentProfile = e.Profile;
                    OnPropertyChanged(nameof(CurrentProfile));

                    // Update UI status flags
                    OnPropertyChanged(nameof(HasUnsavedChanges));
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(CanRedo));
                }
            }
            finally
            {
                _isProcessingExternalUpdate = false;
            }
        }

        private void OnSettingsUpdatedExternal(SettingsUpdatedEvent e)
        {
            if (e.IsFromInitialization) return;

            if (_isProcessingExternalUpdate) return;

            try
            {
                _isProcessingExternalUpdate = true;

                if (e.Settings != null && e.Settings.Id == (_currentSettings?.Id ?? 1))
                {
                    _currentSettings = e.Settings;
                    OnPropertyChanged(nameof(CurrentSettings));
                    _logger.LogInformation("ProfileRepository: Updated settings cache. Protein: {Protein}%",
                        _currentSettings.ProteinPercentage);

                    // Also update the UI status flags
                    OnPropertyChanged(nameof(HasUnsavedChanges));
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(CanRedo));
                }
            }
            finally
            {
                _isProcessingExternalUpdate = false;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _profileSaveCts?.Cancel();
                _profileSaveCts?.Dispose();
                _settingsSaveCts?.Cancel();
                _settingsSaveCts?.Dispose();
                _isDisposed = true;

                GC.SuppressFinalize(this);
            }
        }

        // Add finalizer as safety net
        ~ProfileRepository()
        {
            Dispose();
        }
    }
}
