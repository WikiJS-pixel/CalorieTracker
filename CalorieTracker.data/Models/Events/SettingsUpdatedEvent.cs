namespace CalorieTracker.data.Models.Events
{
    public class SettingsUpdatedEvent
    {
        public UserSettings Settings { get; }
        public bool IsInitialLoad { get; }
        public bool IsFromInitialization { get; }

        public SettingsUpdatedEvent(UserSettings settings, bool isInitialLoad = false)
        {
            Settings = settings;
            IsInitialLoad = isInitialLoad;
        }
    }
}
