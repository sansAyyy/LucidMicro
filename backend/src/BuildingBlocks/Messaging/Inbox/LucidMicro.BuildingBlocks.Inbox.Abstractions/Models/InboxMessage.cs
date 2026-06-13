namespace LucidMicro.BuildingBlocks.Inbox.Abstractions.Models;

public sealed record InboxMessage
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
