using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CalorieTracker.data.Models.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly IProfileRepository _repository;
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<ProfileEditorViewModel> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAppStateService _appStateService;

        private bool _isLoading;

        // Profile Properties
        // Make non-nullable with model defaults to avoid null/zero issues before load
        [ObservableProperty] private string name = "Default User";
        [ObservableProperty] private DateTime birthDate = new DateTime(1990, 1, 1);
        [ObservableProperty] private Gender gender = Gender.Other;
        [ObservableProperty] private double heightCm = 170;
        [ObservableProperty] private ActivityLevel activityLevel = ActivityLevel.ModeratelyActive;
        [ObservableProperty] private WeightGoal weightGoal = WeightGoal.Maintain;

        // Settings Properties
        // defaults to 0 → will be overridden on load
        [ObservableProperty] private bool trackMacros;
        [ObservableProperty] private double proteinPercentage;
        [ObservableProperty] private double carbsPercentage;
        [ObservableProperty] private double fatPercentage;
        [ObservableProperty] private bool useMetricSystem = true;

        public string GoalText => WeightGoal switch
        {
            WeightGoal.Lose => "Goal: Lose Weight",
            WeightGoal.Gain => "Goal: Gain Weight",
            WeightGoal.Maintain => "Goal: Maintain Weight",
            _ => "Goal: Not Set"
        };

        // Status Properties
        public bool CanUndo => _repository.CanUndo;
        public bool CanRedo => _repository.CanRedo;
        public bool IsSaving => _repository.IsSaving;
        public bool HasUnsavedChanges => _repository.HasUnsavedChanges;

        // Gender options for Picker
        public List<Gender> GenderOptions => Enum.GetValues<Gender>().ToList();

        // Activity level radio button properties
        public bool IsSedentary
        {
            get => ActivityLevel == CalorieTracker.data.Models.ActivityLevel.Sedentary;
            set { if (value) ActivityLevel = CalorieTracker.data.Models.ActivityLevel.Sedentary; }
        }

        public bool IsLightlyActive
        {
            get => ActivityLevel == CalorieTracker.data.Models.ActivityLevel.LightlyActive;
            set { if (value) ActivityLevel = CalorieTracker.data.Models.ActivityLevel.LightlyActive; }
        }

        public bool IsModeratelyActive
        {
            get => ActivityLevel == CalorieTracker.data.Models.ActivityLevel.ModeratelyActive;
            set { if (value) ActivityLevel = CalorieTracker.data.Models.ActivityLevel.ModeratelyActive; }
        }

        public bool IsVeryActive
        {
            get => ActivityLevel == CalorieTracker.data.Models.ActivityLevel.VeryActive;
            set { if (value) ActivityLevel = CalorieTracker.data.Models.ActivityLevel.VeryActive; }
        }

        public bool IsExtraActive
        {
            get => ActivityLevel == CalorieTracker.data.Models.ActivityLevel.ExtraActive;
            set { if (value) ActivityLevel = CalorieTracker.data.Models.ActivityLevel.ExtraActive; }
        }

        public ProfileEditorViewModel(
            IProfileRepository repository,
            IUserProfileService userProfileService,
            ILogger<ProfileEditorViewModel> logger,
            IAppStateService appStateService,
            IEventAggregator eventAggregator)
        {
            _repository = repository;
            _userProfileService = userProfileService;
            _logger = logger;
            _appStateService = appStateService;
            _eventAggregator = eventAggregator;

            _logger.LogInformation("ProfileEditorViewModel: Singleton instance created");

            SubscribeToEvents();
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            // Check if wizard just completed
            if (_appStateService.WizardJustCompleted)
            {
                _logger.LogInformation("ProfileEditorViewModel: Wizard just completed, using AppStateService data");

                _appStateService.WizardJustCompleted = false;

                // Use the data saved by wizard
                if (_appStateService.LastSavedProfile != null)
                {
                    Name = _appStateService.LastSavedProfile.Name;
                    BirthDate = _appStateService.LastSavedProfile.BirthDate;
                    Gender = _appStateService.LastSavedProfile.Gender;
                    HeightCm = _appStateService.LastSavedProfile.HeightCm;
                    ActivityLevel = _appStateService.LastSavedProfile.ActivityLevel;
                    WeightGoal = _appStateService.LastSavedProfile.WeightGoal;

                    RefreshActivityRadios();
                    OnPropertyChanged(nameof(GoalText));

                    _logger.LogInformation("ProfileEditorViewModel: Set profile from AppStateService");
                }

                if (_appStateService.LastSavedSettings != null)
                {
                    TrackMacros = _appStateService.LastSavedSettings.TrackMacros;
                    ProteinPercentage = _appStateService.LastSavedSettings.ProteinPercentage;
                    CarbsPercentage = _appStateService.LastSavedSettings.CarbsPercentage;
                    FatPercentage = _appStateService.LastSavedSettings.FatPercentage;
                    UseMetricSystem = _appStateService.LastSavedSettings.UseMetricSystem;

                    _logger.LogInformation("ProfileEditorViewModel: Set settings from AppStateService");
                }

                // Clear the shared state after using it
                _appStateService.LastSavedProfile = null;
                _appStateService.LastSavedSettings = null;
            }
            else
            {
                // Normal load from repository cache
                await LoadFromRepositoryAsync();
            }
        }

        [RelayCommand]
        public async Task UndoAsync()
        {
            await _repository.UndoAsync();
            LoadCurrentValuesAsync();
        }

        [RelayCommand]
        public async Task RedoAsync()
        {
            await _repository.RedoAsync();
            LoadCurrentValuesAsync();
        }

        [RelayCommand]
        public async Task SaveChangesAsync()
        {
            await _repository.SaveChangesAsync();
        }

        private async Task LoadCurrentValuesAsync()
        {
            if (_isLoading) return; // Safety check
            _isLoading = true;

            try
            {
                var profile = await _userProfileService.GetUserProfileAsync();
                if (profile != null)
                {
                    Name = profile.Name ?? "Default User";
                    BirthDate = profile.BirthDate;
                    Gender = profile.Gender;
                    HeightCm = profile.HeightCm;
                    ActivityLevel = profile.ActivityLevel;
                    WeightGoal = profile.WeightGoal;

                    RefreshActivityRadios();
                    OnPropertyChanged(nameof(GoalText));
                }

                var settings = await _userProfileService.GetUserSettingsAsync();
                if (settings != null)
                {
                    TrackMacros = settings.TrackMacros;
                    ProteinPercentage = settings.ProteinPercentage;
                    CarbsPercentage = settings.CarbsPercentage;
                    FatPercentage = settings.FatPercentage;
                    UseMetricSystem = settings.UseMetricSystem;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load fresh profile data");
            }
            finally
            {
                _isLoading = false; // UNLOCK updates
            }
        }

        private void RefreshActivityRadios()
        {
            OnPropertyChanged(nameof(IsSedentary));
            OnPropertyChanged(nameof(IsLightlyActive));
            OnPropertyChanged(nameof(IsModeratelyActive));
            OnPropertyChanged(nameof(IsVeryActive));
            OnPropertyChanged(nameof(IsExtraActive));
        }

        // Property change handlers
        partial void OnNameChanged(string value)
        {
            if (_isLoading) return; 
            UpdateProfile(p => p.Name = value);
        }

        partial void OnBirthDateChanged(DateTime value)
        {
            if (_isLoading) return; // <--- Add this
            UpdateProfile(p => p.BirthDate = value);
        }

        partial void OnGenderChanged(Gender value)
        {
            if (_isLoading) return; // <--- Add this
            UpdateProfile(p => p.Gender = value);
        }

        partial void OnActivityLevelChanged(ActivityLevel value)
        {
            if (_isLoading) return; // <--- Add this
            UpdateProfile(p => p.ActivityLevel = value);
            RefreshActivityRadios();
        }

        partial void OnWeightGoalChanged(WeightGoal value)
        {
            if (_isLoading) return; // <--- Add this
            UpdateProfile(p => p.WeightGoal = value);
            OnPropertyChanged(nameof(GoalText));
        }

        partial void OnHeightCmChanged(double value)
        {
            if (_isLoading) return; // <--- Add this
            if (Math.Abs((_repository.CurrentProfile?.HeightCm ?? 170) - value) > 0.1)
                UpdateProfile(p => p.HeightCm = value);
        }

        partial void OnTrackMacrosChanged(bool value)
        {
            if (_repository.CurrentSettings != null &&
                _repository.CurrentSettings.TrackMacros != value)
            {
                UpdateSettings(s => s.TrackMacros = value);
            }
        }

        partial void OnProteinPercentageChanged(double value)
        {
            if (value < 0 || value > 100)
            {
                ProteinPercentage = _repository.CurrentSettings?.ProteinPercentage ?? 25;
                return;
            }

            if (_repository.CurrentSettings != null &&
                Math.Abs(_repository.CurrentSettings.ProteinPercentage - value) > 0.1)
            {
                UpdateSettings(s => s.ProteinPercentage = value);
            }
        }

        partial void OnCarbsPercentageChanged(double value)
        {
            if (value < 0 || value > 100)
            {
                CarbsPercentage = _repository.CurrentSettings?.CarbsPercentage ?? 25;
                return;
            }

            if (_repository.CurrentSettings != null &&
                Math.Abs(_repository.CurrentSettings.CarbsPercentage - value) > 0.1)
            {
                UpdateSettings(s => s.CarbsPercentage = value);
            }
        }

        partial void OnFatPercentageChanged(double value)
        {
            if (value < 0 || value > 100)
            {
                FatPercentage = _repository.CurrentSettings?.FatPercentage ?? 25;
                return;
            }

            if (_repository.CurrentSettings != null &&
                Math.Abs(_repository.CurrentSettings.FatPercentage - value) > 0.1)
            {
                UpdateSettings(s => s.FatPercentage = value);
            }
        }

        partial void OnUseMetricSystemChanged(bool value)
        {
            if (_repository.CurrentSettings != null &&
                _repository.CurrentSettings.UseMetricSystem != value)
            {
                UpdateSettings(s => s.UseMetricSystem = value);
            }
        }

        private void UpdateProfile(Action<UserProfile> updateAction)
        {
            _ = _repository.UpdateProfileAsync(updateAction)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _logger.LogError(t.Exception?.InnerException ?? t.Exception, "Failed to update profile");
                });
        }

        private void UpdateSettings(Action<UserSettings> updateAction)
        {
            _ = _repository.UpdateSettingsAsync(updateAction)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _logger.LogError(t.Exception?.InnerException ?? t.Exception, "Failed to update settings");
                });
        }

        private async Task LoadFromRepositoryAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                // Load from repository (which caches database data)
                if (_repository.CurrentProfile != null)
                {
                    Name = _repository.CurrentProfile.Name ?? "Default User";
                    BirthDate = _repository.CurrentProfile.BirthDate;
                    Gender = _repository.CurrentProfile.Gender;
                    HeightCm = _repository.CurrentProfile.HeightCm;
                    ActivityLevel = _repository.CurrentProfile.ActivityLevel;
                    WeightGoal = _repository.CurrentProfile.WeightGoal;

                    RefreshActivityRadios();
                    OnPropertyChanged(nameof(GoalText));
                }

                if (_repository.CurrentSettings != null)
                {
                    TrackMacros = _repository.CurrentSettings.TrackMacros;
                    ProteinPercentage = _repository.CurrentSettings.ProteinPercentage;
                    CarbsPercentage = _repository.CurrentSettings.CarbsPercentage;
                    FatPercentage = _repository.CurrentSettings.FatPercentage;
                    UseMetricSystem = _repository.CurrentSettings.UseMetricSystem;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load from repository");
                // Fallback to database if repository fails
                await LoadCurrentValuesAsync();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SubscribeToEvents()
        {
            // Subscribe to profile updates (handle both initial load and updates)
            _eventAggregator.Subscribe<ProfileUpdatedEvent>(async e =>
            {
                if (e.IsFromInitialization) return; // Skip initialization events

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (!_isLoading && e.Profile != null)
                        {
                            _isLoading = true;

                            // Update from event data directly, not from database
                            Name = e.Profile.Name ?? "Default User";
                            BirthDate = e.Profile.BirthDate;
                            Gender = e.Profile.Gender;
                            HeightCm = e.Profile.HeightCm;
                            ActivityLevel = e.Profile.ActivityLevel;
                            WeightGoal = e.Profile.WeightGoal;

                            RefreshActivityRadios();
                            OnPropertyChanged(nameof(GoalText));

                            _isLoading = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load profile after event");
                        _isLoading = false;
                    }
                });
            });

            // Subscribe to settings updates
            _eventAggregator.Subscribe<SettingsUpdatedEvent>(async e =>
            {
                if (e.IsFromInitialization) return; // Skip initialization events

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (e.Settings != null && !_isLoading)
                        {
                            _isLoading = true;

                            // Update settings properties directly
                            TrackMacros = e.Settings.TrackMacros;
                            ProteinPercentage = e.Settings.ProteinPercentage;
                            CarbsPercentage = e.Settings.CarbsPercentage;
                            FatPercentage = e.Settings.FatPercentage;
                            UseMetricSystem = e.Settings.UseMetricSystem;

                            _isLoading = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update settings after event");
                        _isLoading = false;
                    }
                });
            });
        }
    }
}
