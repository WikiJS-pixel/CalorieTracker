using CalorieTracker.data.Interfaces;
using CalorieTracker.data.Models;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class AppStateService : IAppStateService
    {
        private readonly ILogger<AppStateService> _logger;
        private UserProfile? _lastSavedProfile;
        private UserSettings? _lastSavedSettings;
        private bool _wizardJustCompleted;

        public UserProfile? LastSavedProfile
        {
            get => _lastSavedProfile;
            set
            {
                _lastSavedProfile = value;
                _logger?.LogInformation("AppStateService: Set LastSavedProfile - Name: {Name}", value?.Name);
            }
        }

        public UserSettings? LastSavedSettings
        {
            get => _lastSavedSettings;
            set
            {
                _lastSavedSettings = value;
                _logger?.LogInformation("AppStateService: Set LastSavedSettings - Protein: {Protein}%", value?.ProteinPercentage);
            }
        }

        public bool WizardJustCompleted
        {
            get => _wizardJustCompleted;
            set
            {
                _wizardJustCompleted = value;
                _logger?.LogInformation("AppStateService: Set WizardJustCompleted = {Value}", value);
            }
        }

        public AppStateService(ILogger<AppStateService> logger)
        {
            _logger = logger;
            _logger.LogInformation("AppStateService: Created new instance");
        }
    }
}
