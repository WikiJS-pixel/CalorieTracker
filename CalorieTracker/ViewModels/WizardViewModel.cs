using System.ComponentModel.DataAnnotations;
using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieTracker.ViewModels
{
    public partial class WizardViewModel : ObservableValidator
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IWeightService _weightService;
        private readonly IGoalCalculationService _goalCalculationService;

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

        public WizardViewModel(IUserProfileService userProfileService,
            IWeightService weightService,
            IGoalCalculationService goalCalculationService)
        {
            _userProfileService = userProfileService;
            _weightService = weightService;
            _goalCalculationService = goalCalculationService;
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
            if (HasErrors) return;

            try
            {
                // Update or create profile
                var profile = await _userProfileService.GetUserProfileAsync();
                profile.Name = Name;
                profile.BirthDate = BirthDate;
                profile.Gender = Gender;
                profile.HeightCm = HeightCm;
                profile.ActivityLevel = ActivityLevel;
                profile.WeightGoal = WeightGoal;
                profile.WeightChangeRateKgPerWeek = WeightGoal == WeightGoal.Maintain ? 0 : WeightChangeRateKgPerWeek;
                profile.HasCompletedWizard = true;
                profile.WizardCompletedDate = DateTime.UtcNow;

                await _userProfileService.UpdateUserProfileAsync(profile);

                // Update settings (macros)
                var settings = await _userProfileService.GetUserSettingsAsync();
                settings.ProteinPercentage = ProteinPercentage;
                settings.CarbsPercentage = CarbsPercentage;
                settings.FatPercentage = FatPercentage;
                await _userProfileService.UpdateUserSettingsAsync(settings);

                // Create initial weight log
                await _weightService.AddWeightLogAsync(new WeightLog
                {
                    WeightKg = CurrentWeightKg,
                    LogDate = DateTime.UtcNow.Date,
                    Notes = "Initial weight from onboarding"
                });

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
                // TODO: Show error dialog (we'll add in Phase 4)
                System.Diagnostics.Debug.WriteLine($"Wizard save error: {ex}");
            }
        }
    }
}
