using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Outbox.EFCore.Entities;

public sealed class OutboxMessageEntity
{
    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public string? TraceParent { get; private set; }

    public string? TraceState { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? NextRetryAt { get; private set; }

    public DateTimeOffset? DeadAt { get; private set; }

    public int FailureCount { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessageEntity FromMessage(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new OutboxMessageEntity
        {
            Id = message.Id,
            Type = message.Type,
            OccurredAt = message.OccurredAt,
            TraceParent = message.TraceParent,
            TraceState = message.TraceState,
            Payload = message.Payload,
            CreatedAt = message.CreatedAt,
            PublishedAt = message.PublishedAt,
            NextRetryAt = message.NextRetryAt,
            DeadAt = message.DeadAt,
            FailureCount = message.FailureCount,
            LastError = message.LastError
        };
    }

    public OutboxMessage ToMessage()
    {
        return new OutboxMessage
        {
            Id = Id,
            Type = Type,
            OccurredAt = OccurredAt,
            TraceParent = TraceParent,
            TraceState = TraceState,
            Payload = Payload,
            CreatedAt = CreatedAt,
            PublishedAt = PublishedAt,
            NextRetryAt = NextRetryAt,
            DeadAt = DeadAt,
            FailureCount = FailureCount,
            LastError = LastError
        };
    }

    public void MarkAsPublished(DateTimeOffset publishedAt)
    {
        PublishedAt = publishedAt;
        LockedUntil = null;
        NextRetryAt = null;
        DeadAt = null;
        LastError = null;
    }

    public void MarkAsLocked(DateTimeOffset lockedUntil)
    {
        LockedUntil = lockedUntil;
    }

    public void MarkAsFailed(
        string error,
        DateTimeOffset? nextRetryAt,
        DateTimeOffset? deadAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        FailureCount++;
        LockedUntil = null;
        NextRetryAt = nextRetryAt;
        DeadAt = deadAt;
        LastError = error;
    }
}
