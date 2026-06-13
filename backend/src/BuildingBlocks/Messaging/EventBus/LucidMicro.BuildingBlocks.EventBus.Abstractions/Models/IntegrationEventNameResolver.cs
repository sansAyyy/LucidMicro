using System.Reflection;

namespace LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

public static class IntegrationEventNameResolver
{
    public static string Resolve<TEvent>()
        where TEvent : IntegrationEvent
    {
        return Resolve(typeof(TEvent));
    }

    public static string Resolve(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        if (!typeof(IntegrationEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException("eventType must inherit from IntegrationEvent.", nameof(eventType));
        }

        return eventType.GetCustomAttribute<IntegrationEventNameAttribute>()?.Name
            ?? eventType.Name;
    }
}
