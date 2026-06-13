using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;

public interface IEventBus
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
