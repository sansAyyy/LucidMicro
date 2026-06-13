namespace LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;

public sealed record OutboxMessage
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public string? TraceParent { get; init; }

    public string? TraceState { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PublishedAt { get; init; }

    public DateTimeOffset? NextRetryAt { get; init; }

    public DateTimeOffset? DeadAt { get; init; }

    public int FailureCount { get; init; }

    public string? LastError { get; init; }
}
