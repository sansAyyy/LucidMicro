using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestOutboxMessageStore : IOutboxMessageStore
{
    private readonly List<OutboxMessage> _items = [];

    public IReadOnlyList<OutboxMessage> Items => _items;

    public Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        _items.Add(message);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<OutboxMessage>>(_items.Take(maxCount).ToArray());
    }

    public Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset? nextRetryAt,
        DateTimeOffset? deadAt,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }
}
