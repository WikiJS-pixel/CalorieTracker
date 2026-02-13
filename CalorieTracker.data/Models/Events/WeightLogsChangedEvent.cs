namespace CalorieTracker.data.Models.Events
{
    public class WeightLogsChangedEvent
    {
        public List<WeightLog> WeightLogs { get; }
        public WeightLog? AddedOrUpdated { get; }
        public int? DeletedId { get; }

        public WeightLogsChangedEvent(List<WeightLog> weightLogs,
            WeightLog? addedOrUpdated = null,
            int? deletedId = null)
        {
            WeightLogs = weightLogs;
            AddedOrUpdated = addedOrUpdated;
            DeletedId = deletedId;
        }
    }
}
