using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Serialization;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Services;
using LucidMicro.BuildingBlocks.Outbox.EFCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Entities;
using LucidMicro.BuildingBlocks.Outbox.EFCore.ModelBuilding;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Options;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Stores;
using LucidMicro.Tests.Shared.Time;
using LucidMicro.Tests.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class OutboxEfCoreTests
{
    [Fact]
    public async Task ConfigureOutbox_ConfiguresTableAndColumns()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(OutboxMessageEntity));
        Assert.NotNull(entityType);

        var table = StoreObjectIdentifier.Table(OutboxModelBuilderExtensions.TableName, null);

        Assert.Equal(OutboxModelBuilderExtensions.TableName, entityType.GetTableName());
        Assert.Equal("id", GetColumnName(entityType, nameof(OutboxMessageEntity.Id), table));
        Assert.Equal("type", GetColumnName(entityType, nameof(OutboxMessageEntity.Type), table));
        Assert.Equal("occurred_at", GetColumnName(entityType, nameof(OutboxMessageEntity.OccurredAt), table));
        Assert.Equal("trace_parent", GetColumnName(entityType, nameof(OutboxMessageEntity.TraceParent), table));
        Assert.Equal("trace_state", GetColumnName(entityType, nameof(OutboxMessageEntity.TraceState), table));
        Assert.Equal("payload", GetColumnName(entityType, nameof(OutboxMessageEntity.Payload), table));
        Assert.Equal("created_at", GetColumnName(entityType, nameof(OutboxMessageEntity.CreatedAt), table));
        Assert.Equal("published_at", GetColumnName(entityType, nameof(OutboxMessageEntity.PublishedAt), table));
        Assert.Equal("locked_until", GetColumnName(entityType, nameof(OutboxMessageEntity.LockedUntil), table));
        Assert.Equal("next_retry_at", GetColumnName(entityType, nameof(OutboxMessageEntity.NextRetryAt), table));
        Assert.Equal("dead_at", GetColumnName(entityType, nameof(OutboxMessageEntity.DeadAt), table));
        Assert.Equal("failure_count", GetColumnName(entityType, nameof(OutboxMessageEntity.FailureCount), table));
        Assert.Equal("last_error", GetColumnName(entityType, nameof(OutboxMessageEntity.LastError), table));
    }

    [Fact]
    public async Task ClaimPendingAsync_ReturnsUnpublishedMessagesOrderedByCreatedAt()
    {
        await using var scope = await CreateContextScopeAsync();
        var store = new EfCoreOutboxMessageStore<TestOutboxDbContext>(scope.Context);
        var first = CreateMessage("first", createdAt: DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"));
        var second = CreateMessage("second", createdAt: DateTimeOffset.Parse("2026-05-26T00:01:00+00:00"));
        var published = CreateMessage(
            "published",
            createdAt: DateTimeOffset.Parse("2026-05-26T00:02:00+00:00"),
            publishedAt: DateTimeOffset.Parse("2026-05-26T00:03:00+00:00"));

        await store.AddAsync(second);
        await store.AddAsync(published);
        await store.AddAsync(first);
        await store.SaveChangesAsync();

        var pendingMessages = await store.ClaimPendingAsync(maxCount: 10);

        Assert.Collection(
            pendingMessages,
            message => Assert.Equal(first.Id, message.Id),
            message => Assert.Equal(second.Id, message.Id));
    }

    [Fact]
    public async Task ClaimPendingAsync_ClaimsReturnedMessages()
    {
        var now = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        await using var scope = await CreateContextScopeAsync();
        var store = CreateStore(scope.Context, now);
        var message = CreateMessage("message");

        await store.AddAsync(message);
        await store.SaveChangesAsync();

        var pendingMessages = await store.ClaimPendingAsync(maxCount: 1);

        Assert.Equal(message.Id, Assert.Single(pendingMessages).Id);

        var storedEntity = await scope.Context.OutboxMessages.SingleAsync();
        Assert.Equal(now.AddMinutes(5), storedEntity.LockedUntil);
    }

    [Fact]
    public async Task ClaimPendingAsync_SkipsLockedMessages()
    {
        var now = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        await using var scope = await CreateContextScopeAsync();
        var store = CreateStore(scope.Context, now);
        var first = CreateMessage("first", createdAt: DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"));
        var second = CreateMessage("second", createdAt: DateTimeOffset.Parse("2026-05-26T00:01:00+00:00"));

        await store.AddAsync(first);
        await store.AddAsync(second);
        await store.SaveChangesAsync();

        var firstBatch = await store.ClaimPendingAsync(maxCount: 1);
        var secondBatch = await store.ClaimPendingAsync(maxCount: 1);

        Assert.Equal(first.Id, Assert.Single(firstBatch).Id);
        Assert.Equal(second.Id, Assert.Single(secondBatch).Id);
    }

    [Fact]
    public async Task ClaimPendingAsync_SkipsMessagesWaitingForRetryOrDead()
    {
        var now = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        await using var scope = await CreateContextScopeAsync();
        var store = CreateStore(scope.Context, now);
        var ready = CreateMessage("ready", createdAt: DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"));
        var waiting = CreateMessage("waiting", createdAt: DateTimeOffset.Parse("2026-05-26T00:01:00+00:00"));
        var dead = CreateMessage("dead", createdAt: DateTimeOffset.Parse("2026-05-26T00:02:00+00:00"));

        await store.AddAsync(ready);
        await store.AddAsync(waiting);
        await store.AddAsync(dead);
        await store.SaveChangesAsync();

        await store.MarkAsFailedAsync(waiting.Id, "waiting", now.AddMinutes(1), null);
        await store.MarkAsFailedAsync(dead.Id, "dead", null, now);
        await store.SaveChangesAsync();

        var pendingMessages = await store.ClaimPendingAsync(maxCount: 10);

        Assert.Equal(ready.Id, Assert.Single(pendingMessages).Id);
    }

    [Fact]
    public async Task MarkAsPublishedAsync_MarksMessageAsPublished()
    {
        await using var scope = await CreateContextScopeAsync();
        var store = new EfCoreOutboxMessageStore<TestOutboxDbContext>(scope.Context);
        var message = CreateMessage("message");
        var publishedAt = DateTimeOffset.Parse("2026-05-26T01:00:00+00:00");

        await store.AddAsync(message);
        await store.SaveChangesAsync();

        await store.MarkAsPublishedAsync(message.Id, publishedAt);
        await store.SaveChangesAsync();

        var storedEntity = await scope.Context.OutboxMessages.SingleAsync();
        Assert.Equal(publishedAt, storedEntity.PublishedAt);
        Assert.Null(storedEntity.NextRetryAt);
        Assert.Null(storedEntity.DeadAt);
        Assert.Null(storedEntity.LastError);
    }

    [Fact]
    public async Task MarkAsFailedAsync_IncrementsFailureCountAndStoresRetryState()
    {
        await using var scope = await CreateContextScopeAsync();
        var store = new EfCoreOutboxMessageStore<TestOutboxDbContext>(scope.Context);
        var message = CreateMessage("message");
        var nextRetryAt = DateTimeOffset.Parse("2026-05-26T00:01:00+00:00");

        await store.AddAsync(message);
        await store.SaveChangesAsync();

        await store.MarkAsFailedAsync(message.Id, "publish failed", nextRetryAt, null);
        await store.SaveChangesAsync();

        var storedEntity = await scope.Context.OutboxMessages.SingleAsync();
        Assert.Equal(1, storedEntity.FailureCount);
        Assert.Equal(nextRetryAt, storedEntity.NextRetryAt);
        Assert.Null(storedEntity.DeadAt);
        Assert.Equal("publish failed", storedEntity.LastError);
    }

    [Fact]
    public async Task MarkAsFailedAsync_CanMarkMessageAsDead()
    {
        var deadAt = DateTimeOffset.Parse("2026-05-26T00:01:00+00:00");
        await using var scope = await CreateContextScopeAsync();
        var store = new EfCoreOutboxMessageStore<TestOutboxDbContext>(scope.Context);
        var message = CreateMessage("message");

        await store.AddAsync(message);
        await store.SaveChangesAsync();

        await store.MarkAsFailedAsync(message.Id, "publish failed", null, deadAt);
        await store.SaveChangesAsync();

        var storedEntity = await scope.Context.OutboxMessages.SingleAsync();
        Assert.Equal(1, storedEntity.FailureCount);
        Assert.Null(storedEntity.NextRetryAt);
        Assert.Equal(deadAt, storedEntity.DeadAt);
        Assert.Equal("publish failed", storedEntity.LastError);
    }

    [Fact]
    public void AddLucidEfCoreOutbox_RegistersStoreAndSerializer()
    {
        var services = new ServiceCollection();

        services.AddLucidEfCoreOutbox<TestOutboxDbContext>();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IOutboxMessageStore)
                       && service.ImplementationType == typeof(EfCoreOutboxMessageStore<TestOutboxDbContext>));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IOutboxEventWriter)
                       && service.ImplementationType == typeof(DefaultOutboxEventWriter));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IOutboxMessageSerializer)
                       && service.ImplementationType == typeof(SystemTextJsonOutboxMessageSerializer));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(EfCoreOutboxOptions));
    }

    private static string? GetColumnName(
        IEntityType entityType,
        string propertyName,
        StoreObjectIdentifier table)
    {
        return entityType.FindProperty(propertyName)?.GetColumnName(table);
    }

    private static OutboxMessage CreateMessage(
        string name,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? publishedAt = null)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = $"test.{name}.v1",
            OccurredAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            Payload = $$"""{"name":"{{name}}"}""",
            CreatedAt = createdAt ?? DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            PublishedAt = publishedAt
        };
    }

    private static Task<SqliteDbContextScope<TestOutboxDbContext>> CreateContextScopeAsync()
    {
        return SqliteDbContextScope<TestOutboxDbContext>.CreateAsync(connection =>
        {
            var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
                .UseSqlite(connection)
                .Options;

            return new TestOutboxDbContext(options);
        });
    }

    private static EfCoreOutboxMessageStore<TestOutboxDbContext> CreateStore(
        TestOutboxDbContext dbContext,
        DateTimeOffset now)
    {
        return new EfCoreOutboxMessageStore<TestOutboxDbContext>(
            dbContext,
            new TestTimeProvider(now),
            new EfCoreOutboxOptions());
    }

    private sealed class TestOutboxDbContext : DbContext
    {
        public TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options)
            : base(options)
        {
        }

        public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureOutbox();
        }
    }
}
