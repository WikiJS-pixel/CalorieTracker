using CalorieTracker.data.Models;

namespace CalorieTracker.data.Interfaces
{
    public interface IAppStateService
    {
        UserProfile? LastSavedProfile { get; set; }
        UserSettings? LastSavedSettings { get; set; }
        bool WizardJustCompleted { get; set; }
    }
}
