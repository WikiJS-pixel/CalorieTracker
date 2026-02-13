namespace CalorieTracker.data.Interfaces
{
    public interface IEventAggregator
    {
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
    }
}
