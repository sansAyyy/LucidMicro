using System.Diagnostics;
using System.Text.Json;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Internal;

internal sealed class RabbitMqIntegrationEventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IntegrationEventEnvelope CreateEnvelope<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return new IntegrationEventEnvelope
        {
            Id = integrationEvent.Id,
            Type = IntegrationEventNameResolver.Resolve<TEvent>(),
            OccurredAt = integrationEvent.OccurredAt,
            TraceParent = Activity.Current?.Id,
            TraceState = Activity.Current?.TraceStateString,
            Payload = JsonSerializer.Serialize(integrationEvent, typeof(TEvent), JsonOptions)
        };
    }

    public ReadOnlyMemory<byte> SerializeEnvelope(IntegrationEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
    }

    public IntegrationEventEnvelope DeserializeEnvelope(ReadOnlyMemory<byte> body)
    {
        var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(body.Span, JsonOptions);

        return envelope ?? throw new InvalidOperationException("Integration event envelope cannot be deserialized.");
    }

    public IntegrationEvent DeserializeEvent(IntegrationEventEnvelope envelope, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(eventType);

        if (!typeof(IntegrationEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException("eventType must inherit from IntegrationEvent.", nameof(eventType));
        }

        var integrationEvent = JsonSerializer.Deserialize(envelope.Payload, eventType, JsonOptions);

        return integrationEvent as IntegrationEvent
            ?? throw new InvalidOperationException("Integration event payload cannot be deserialized.");
    }
}
