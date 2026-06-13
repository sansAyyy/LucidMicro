using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;

public interface IOutboxMessageStore
{
    Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset? nextRetryAt,
        DateTimeOffset? deadAt,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
