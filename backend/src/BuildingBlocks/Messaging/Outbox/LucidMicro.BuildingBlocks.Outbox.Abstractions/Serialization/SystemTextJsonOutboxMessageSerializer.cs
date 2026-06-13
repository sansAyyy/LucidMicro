using System.Diagnostics;
using System.Text.Json;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Outbox.Abstractions.Serialization;

public sealed class SystemTextJsonOutboxMessageSerializer : IOutboxMessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OutboxMessage Serialize<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return new OutboxMessage
        {
            Id = integrationEvent.Id,
            Type = IntegrationEventNameResolver.Resolve<TEvent>(),
            OccurredAt = integrationEvent.OccurredAt,
            TraceParent = Activity.Current?.Id,
            TraceState = Activity.Current?.TraceStateString,
            Payload = JsonSerializer.Serialize(integrationEvent, typeof(TEvent), JsonOptions)
        };
    }
}
