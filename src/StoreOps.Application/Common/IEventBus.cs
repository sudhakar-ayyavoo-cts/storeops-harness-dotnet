using StoreOps.Domain.Events;

namespace StoreOps.Application.Common;

public interface IEventBus
{
    void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent;
}
