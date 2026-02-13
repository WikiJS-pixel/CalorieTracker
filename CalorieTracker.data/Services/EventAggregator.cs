using System.Collections.Concurrent;
using CalorieTracker.data.Interfaces;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.data.Services
{
    public class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
        private readonly ILogger<EventAggregator> _logger;

        public EventAggregator(ILogger<EventAggregator> logger)
        {
            _logger = logger;
        }

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            var eventType = typeof(TEvent);
            var handlers = _handlers.GetOrAdd(eventType, _ => []);

            lock (handlers)
            {
                handlers.Add(handler);
            }

            _logger.LogDebug("Subscribed to {EventType}, total handlers: {Count}",
                eventType.Name, handlers.Count);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            var eventType = typeof(TEvent);

            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                lock (handlers)
                {
                    handlers.Remove(handler);
                }

                _logger.LogDebug("Unsubscribed from {EventType}, remaining handlers: {Count}",
                    eventType.Name, handlers.Count);
            }
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
        {
            var eventType = typeof(TEvent);

            if (!_handlers.TryGetValue(eventType, out var handlers) || !handlers.Any())
            {
                _logger.LogTrace("No handlers for {EventType}", eventType.Name);
                return;
            }

            _logger.LogDebug("Publishing {EventType} to {Count} handlers",
                eventType.Name, handlers.Count);

            // Clone handlers to avoid modification during invocation
            List<Delegate> handlersToInvoke;
            lock (handlers)
            {
                handlersToInvoke = new List<Delegate>(handlers);
            }

            // Invoke handlers asynchronously
            var tasks = handlersToInvoke
                .OfType<Action<TEvent>>()
                .Select(handler => Task.Run(() =>
                {
                    try
                    {
                        handler(@event);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in event handler for {EventType}", eventType.Name);
                    }
                }))
                .ToList();

            await Task.WhenAll(tasks);
        }
    }
}
