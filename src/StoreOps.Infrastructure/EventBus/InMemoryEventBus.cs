using StoreOps.Application.Common;
using StoreOps.Domain.Events;

namespace StoreOps.Infrastructure.EventBus;

internal sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();

    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers)
        {
            handler(domainEvent!).GetAwaiter().GetResult();
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Func<object, Task>>();
        }

        _handlers[eventType].Add(obj => handler((TEvent)obj));
    }
}
