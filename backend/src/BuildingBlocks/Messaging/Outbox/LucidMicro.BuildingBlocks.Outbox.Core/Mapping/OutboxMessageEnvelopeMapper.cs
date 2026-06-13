using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Outbox.Core.Mapping;

internal static class OutboxMessageEnvelopeMapper
{
    public static IntegrationEventEnvelope Map(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new IntegrationEventEnvelope
        {
            Id = message.Id,
            Type = message.Type,
            OccurredAt = message.OccurredAt,
            TraceParent = message.TraceParent,
            TraceState = message.TraceState,
            Payload = message.Payload
        };
    }
}
