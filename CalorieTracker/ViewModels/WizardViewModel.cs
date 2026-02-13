using System.ComponentModel.DataAnnotations;
using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CalorieTracker.data.Models.Events;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.ViewModels
{
    public partial class WizardViewModel : ObservableValidator
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IWeightService _weightService;
        private readonly IGoalCalculationService _goalCalculationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IErrorService _errorService;
        private readonly IAppStateService _appStateService;
        private readonly ILogger<WizardViewModel> _logger;

        // Steps: 0=Personal, 1=Physical, 2=Activity&Goals, 3=Dietary, 4=Preview
        [ObservableProperty]
        private int _currentStep = 0;

        [ObservableProperty]
        private string _nextButtonText = "Next";

        // Conditional UI
        public bool ShowWeightRate => WeightGoal != WeightGoal.Maintain;

        partial void OnWeightGoalChanged(WeightGoal oldValue, WeightGoal newValue)
        {
            OnPropertyChanged(nameof(ShowWeightRate));
            if (newValue != WeightGoal.Maintain && WeightChangeRateKgPerWeek == 0)
                WeightChangeRateKgPerWeek = 0.5; // sensible default
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        // Enum options for Pickers
        public IReadOnlyList<Gender> GenderOptions => Enum.GetValues<Gender>();
        public IReadOnlyList<ActivityLevel> ActivityLevelOptions => Enum.GetValues<ActivityLevel>();
        public IReadOnlyList<WeightGoal> WeightGoalOptions => Enum.GetValues<WeightGoal>();

        // Step 0: Personal Info
        [ObservableProperty]
        [Required(ErrorMessage = "Name is required")]
        [MinLength(2)]
        private string _name = "Default User";

        [ObservableProperty]
        [Required]
        private DateTime _birthDate = new DateTime(1990, 1, 1);
        public DateTime Today => DateTime.Today;
        public DateTime MinBirthDate => DateTime.Today.AddYears(-120); // Safer than -100

        [ObservableProperty]
        [Required]
        private Gender _gender = Gender.Other;

        partial void OnBirthDateChanged(DateTime oldValue, DateTime newValue)
        {
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        partial void OnGenderChanged(Gender oldValue, Gender newValue)
        {
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        // Step 1: Physical Stats
        [ObservableProperty]
        [Range(50, 250, ErrorMessage = "Height must be 50-250 cm")]
        private double _heightCm = 170;

        [ObservableProperty]
        [Range(20, 300, ErrorMessage = "Weight must be 20-300 kg")]
        private double _currentWeightKg = 70;

        partial void OnHeightCmChanged(double oldValue, double newValue)
        {
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        partial void OnCurrentWeightKgChanged(double oldValue, double newValue)
        {
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        // Step 2: Activity & Goals
        [ObservableProperty]
        private ActivityLevel _activityLevel = ActivityLevel.ModeratelyActive;

        [ObservableProperty]
        private WeightGoal _weightGoal = WeightGoal.Maintain;

        [ObservableProperty]
        [Range(-2.0, 2.0)]
        private double _weightChangeRateKgPerWeek = 0.0; // 0 for maintain

        partial void OnActivityLevelChanged(ActivityLevel oldValue, ActivityLevel newValue)
        {
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        partial void OnWeightChangeRateKgPerWeekChanged(double oldValue, double newValue)
        {
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        // Step 3: Dietary Preferences (macros)
        [ObservableProperty]
        [Range(0, 100)]
        private double _proteinPercentage = 25;

        [ObservableProperty]
        [Range(0, 100)]
        private double _carbsPercentage = 50;

        [ObservableProperty]
        [Range(0, 100)]
        private double _fatPercentage = 25;

        [ObservableProperty]
        private string _macroTotal = "100%";

        partial void OnProteinPercentageChanged(double oldValue, double newValue) => UpdateMacroTotal();
        partial void OnCarbsPercentageChanged(double oldValue, double newValue) => UpdateMacroTotal();
        partial void OnFatPercentageChanged(double oldValue, double newValue) => UpdateMacroTotal();

        private void UpdateMacroTotal()
        {
            var total = ProteinPercentage + CarbsPercentage + FatPercentage;
            MacroTotal = $"{total:F0}%";
            if (Math.Abs(total - 100) > 0.01)
                MacroTotal += " (will be normalized)";
            if (CurrentStep == 4) CalculatePreviewGoals();
        }

        // Step 4: Preview (calculated)
        [ObservableProperty]
        private DailyGoals? _previewGoals;

        public WizardViewModel(
            IUserProfileService userProfileService,
            IWeightService weightService,
            IGoalCalculationService goalCalculationService,
            IEventAggregator eventAggregator,
            IErrorService errorService,
            IAppStateService appStateService,
            ILogger<WizardViewModel> logger)
        {
            _userProfileService = userProfileService;
            _weightService = weightService;
            _goalCalculationService = goalCalculationService;
            _eventAggregator = eventAggregator;
            _errorService = errorService;
            _appStateService = appStateService;
            _logger = logger;

            UpdateMacroTotal();
        }

        partial void OnCurrentStepChanged(int oldValue, int newValue)
        {
            NextButtonText = newValue == 4 ? "Finish" : "Next";
            if (newValue == 4)
            {
                CalculatePreviewGoals();
            }
        }

        private void CalculatePreviewGoals()
        {
            try
            {
                int age = CalculateAge(BirthDate);

                // Mifflin-St Jeor BMR
                double bmr = Gender switch
                {
                    Gender.Male => 10 * CurrentWeightKg + 6.25 * HeightCm - 5 * age + 5,
                    Gender.Female => 10 * CurrentWeightKg + 6.25 * HeightCm - 5 * age - 161,
                    _ => (10 * CurrentWeightKg + 6.25 * HeightCm - 5 * age + 5 + 10 * CurrentWeightKg + 6.25 * HeightCm - 5 * age - 161) / 2
                };

                // Activity multipliers (standard values)
                double multiplier = ActivityLevel switch
                {
                    ActivityLevel.Sedentary => 1.2,
                    ActivityLevel.LightlyActive => 1.375,
                    ActivityLevel.ModeratelyActive => 1.55,
                    ActivityLevel.VeryActive => 1.725,
                    ActivityLevel.ExtraActive => 1.9,
                    _ => 1.55
                };

                double tdee = bmr * multiplier;

                // Goal adjustment (~1000 kcal per kg/week)
                double dailyAdjustment = 0;
                if (WeightGoal != WeightGoal.Maintain)
                {
                    double weeklyCalories = WeightChangeRateKgPerWeek * 1000; // approx 7700kcal/kg → ~1100, but 1000 is common
                    dailyAdjustment = WeightGoal == WeightGoal.Lose ? -weeklyCalories / 7 : weeklyCalories / 7;
                }

                double targetCalories = Math.Round(tdee + dailyAdjustment);

                // Macros in grams
                double proteinG = Math.Round(targetCalories * ProteinPercentage / 100 / 4);
                double carbsG = Math.Round(targetCalories * CarbsPercentage / 100 / 4);
                double fatG = Math.Round(targetCalories * FatPercentage / 100 / 9);

                PreviewGoals = new DailyGoals
                {
                    TargetCalories = targetCalories,
                    TargetProtein = proteinG,
                    TargetCarbs = carbsG,
                    TargetFat = fatG
                    // Add TargetProteinPercentage etc. if your DailyGoals has them
                };
            }
            catch
            {
                PreviewGoals = null;
            }
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.UtcNow;
            int age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age < 0 ? 0 : age;
        }

        [RelayCommand]
        private void Back()
        {
            if (CurrentStep > 0)
                CurrentStep--;
        }

        [RelayCommand]
        private async Task NextAsync()
        {
            ValidateAllProperties();
            if (HasErrors) return;

            // Normalize macros if on dietary step
            if (CurrentStep == 3)
            {
                var total = ProteinPercentage + CarbsPercentage + FatPercentage;
                if (Math.Abs(total - 100) > 0.01)
                {
                    ProteinPercentage = Math.Round(ProteinPercentage / total * 100);
                    CarbsPercentage = Math.Round(CarbsPercentage / total * 100);
                    FatPercentage = Math.Round(FatPercentage / total * 100);
                    UpdateMacroTotal();
                }
            }

            if (CurrentStep < 4)
            {
                CurrentStep++;
            }
            else
            {
                await FinishAsync();
            }
        }

        private async Task FinishAsync()
        {
            // Final validation
            ValidateAllProperties();
            if (HasErrors)
            {
                await _errorService.ShowErrorAsync(
                    "Please fix the validation errors before continuing",
                    "Validation Failed");
                return;
            }

            try
            {
                // Get or create profile
                var profile = await _userProfileService.GetUserProfileAsync();
                if (profile == null)
                {
                    profile = new UserProfile
                    {
                        Id = 1,
                        CreatedDate = DateTime.UtcNow
                    }; // New profile for first-time users
                       // Add any other defaults here if needed (e.g., profile.SomeField = defaultValue;)
                }

                // Apply wizard values
                profile.Name = Name;
                profile.BirthDate = BirthDate;
                profile.Gender = Gender;
                profile.HeightCm = HeightCm;
                profile.ActivityLevel = ActivityLevel;
                profile.WeightGoal = WeightGoal;
                profile.WeightChangeRateKgPerWeek = WeightGoal == WeightGoal.Maintain ? 0 : WeightChangeRateKgPerWeek;
                profile.HasCompletedWizard = true;
                profile.WizardCompletedDate = DateTime.UtcNow;

                _logger.LogInformation("Wizard: Saving profile - Name: {Name}, Height: {Height}", profile.Name, profile.HeightCm);
                var savedProfile = await _userProfileService.UpdateUserProfileAsync(profile);

                _logger.LogInformation("Wizard: Publishing ProfileUpdatedEvent");
                await _eventAggregator.PublishAsync(new ProfileUpdatedEvent(savedProfile));

                // Get or create settings
                var settings = await _userProfileService.GetUserSettingsAsync();
                if (settings == null)
                {
                    settings = new UserSettings
                    {
                        Id = 1,
                        CreatedDate = DateTime.UtcNow
                    };
                }

                // Apply macro preferences and sensible defaults
                settings.TrackMacros = true; // Wizard configures macros, so enable tracking
                settings.ProteinPercentage = ProteinPercentage;
                settings.CarbsPercentage = CarbsPercentage;
                settings.FatPercentage = FatPercentage;
                settings.UseMetricSystem = true; // Wizard uses metric; match your app's default
                settings.LastUpdatedDate = DateTime.UtcNow;

                _logger.LogInformation("Wizard: Saving settings - Protein: {Protein}%, Carbs: {Carbs}%",
            settings.ProteinPercentage, settings.CarbsPercentage);
                var savedSettings = await _userProfileService.UpdateUserSettingsAsync(settings);

                _logger.LogInformation("Wizard: Publishing SettingsUpdatedEvent");
                await _eventAggregator.PublishAsync(new SettingsUpdatedEvent(savedSettings));

                // Create initial weight log
                await _weightService.AddWeightLogAsync(new WeightLog
                {
                    WeightKg = CurrentWeightKg,
                    LogDate = DateTime.UtcNow.Date,
                    Notes = "Initial weight from onboarding"
                });

                await _errorService.ShowSuccessAsync(
                "Your profile has been set up successfully!",
                "Setup Complete");

                // Store in shared state
                _appStateService.LastSavedProfile = savedProfile;
                _appStateService.LastSavedSettings = savedSettings;
                _appStateService.WizardJustCompleted = true;

                // Switch to main app
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                });
            }
            catch (Exception ex)
            {
                await _errorService.ShowErrorAsync(
                    $"Failed to save profile: {ex.Message}",
                    "Save Error");
            }
        }
    }
}
