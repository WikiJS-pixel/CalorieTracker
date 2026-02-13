namespace CalorieTracker.data.Models.Events
{
    public class ProfileUpdatedEvent
    {
        public UserProfile Profile { get; }
        public bool IsInitialLoad { get; }
        public bool IsFromInitialization { get; }

        public ProfileUpdatedEvent(UserProfile profile, bool isInitialLoad = false)
        {
            Profile = profile;
            IsInitialLoad = isInitialLoad;
        }
    }
}
