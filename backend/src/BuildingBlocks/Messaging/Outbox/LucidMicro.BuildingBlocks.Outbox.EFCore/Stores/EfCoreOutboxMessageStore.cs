using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Entities;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Options;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Outbox.EFCore.Stores;

public sealed class EfCoreOutboxMessageStore<TDbContext> : IOutboxMessageStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly EfCoreOutboxOptions _options;

    public EfCoreOutboxMessageStore(TDbContext dbContext)
        : this(dbContext, TimeProvider.System, new EfCoreOutboxOptions())
    {
    }

    public EfCoreOutboxMessageStore(
        TDbContext dbContext,
        TimeProvider timeProvider,
        EfCoreOutboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _options = options;
    }

    public async Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await _dbContext
            .Set<OutboxMessageEntity>()
            .AddAsync(OutboxMessageEntity.FromMessage(message), cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);

        var now = _timeProvider.GetUtcNow();
        var lockedUntil = now.Add(_options.LockDuration);

        if (IsNpgsqlProvider())
        {
            return await ClaimPendingWithPostgreSqlAsync(
                maxCount,
                now,
                lockedUntil,
                cancellationToken);
        }

        var query = _dbContext
            .Set<OutboxMessageEntity>()
            .Where(message => message.PublishedAt == null
                              && message.DeadAt == null);

        OutboxMessageEntity[] entities;

        if (IsSqliteProvider())
        {
            // SQLite cannot translate DateTimeOffset ordering; this keeps lightweight tests usable.
            entities = (await query.ToArrayAsync(cancellationToken))
                .Where(message => (message.LockedUntil is null || message.LockedUntil < now)
                                  && (message.NextRetryAt is null || message.NextRetryAt <= now))
                .OrderBy(message => message.CreatedAt)
                .Take(maxCount)
                .ToArray();
        }
        else
        {
            entities = await query
                .Where(message => message.LockedUntil == null || message.LockedUntil < now)
                .Where(message => message.NextRetryAt == null || message.NextRetryAt <= now)
                .OrderBy(message => message.CreatedAt)
                .Take(maxCount)
                .ToArrayAsync(cancellationToken);
        }

        foreach (var entity in entities)
        {
            entity.MarkAsLocked(lockedUntil);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entities
            .Select(entity => entity.ToMessage())
            .ToArray();
    }

    public async Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(messageId, cancellationToken);

        entity.MarkAsPublished(publishedAt);
    }

    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset? nextRetryAt,
        DateTimeOffset? deadAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        var entity = await GetRequiredEntityAsync(messageId, cancellationToken);

        entity.MarkAsFailed(error, nextRetryAt, deadAt);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<OutboxMessageEntity> GetRequiredEntityAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext
            .Set<OutboxMessageEntity>()
            .FindAsync([messageId], cancellationToken);

        return entity
            ?? throw new InvalidOperationException($"Outbox message '{messageId}' was not found.");
    }

    private bool IsSqliteProvider()
    {
        return _dbContext.Database.ProviderName?.Contains(
            "Sqlite",
            StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool IsNpgsqlProvider()
    {
        return _dbContext.Database.ProviderName?.Contains(
            "Npgsql",
            StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimPendingWithPostgreSqlAsync(
        int maxCount,
        DateTimeOffset now,
        DateTimeOffset lockedUntil,
        CancellationToken cancellationToken)
    {
        var entities = await _dbContext
            .Set<OutboxMessageEntity>()
            .FromSqlInterpolated($"""
                with claimed as (
                    select id
                    from outbox_messages
                    where published_at is null
                      and dead_at is null
                      and (locked_until is null or locked_until < {now})
                      and (next_retry_at is null or next_retry_at <= {now})
                    order by created_at
                    limit {maxCount}
                    for update skip locked
                )
                update outbox_messages message
                set locked_until = {lockedUntil}
                from claimed
                where message.id = claimed.id
                returning message.*
                """)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        return entities
            .OrderBy(entity => entity.CreatedAt)
            .Select(entity => entity.ToMessage())
            .ToArray();
    }
}
