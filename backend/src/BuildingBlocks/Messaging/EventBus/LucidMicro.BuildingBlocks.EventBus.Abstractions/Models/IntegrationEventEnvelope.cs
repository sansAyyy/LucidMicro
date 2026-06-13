namespace LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

public sealed record IntegrationEventEnvelope
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public string? TraceParent { get; init; }

    public string? TraceState { get; init; }

    public required string Payload { get; init; }
}
