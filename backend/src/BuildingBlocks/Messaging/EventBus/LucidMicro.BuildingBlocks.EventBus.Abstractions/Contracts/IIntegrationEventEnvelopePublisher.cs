using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;

public interface IIntegrationEventEnvelopePublisher
{
    Task PublishAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default);
}
