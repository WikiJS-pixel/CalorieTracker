namespace CalorieTracker.data.Models.Events
{
    public class GoalsRecalculatedEvent
    {
        public DailyGoals Goals { get; }
        public DateTime Timestamp { get; }

        public GoalsRecalculatedEvent(DailyGoals goals)
        {
            Goals = goals;
            Timestamp = DateTime.UtcNow;
        }
    }
}
