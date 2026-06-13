using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Abstractions.Models;
using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using LucidMicro.BuildingBlocks.Inbox.EFCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Entities;
using LucidMicro.BuildingBlocks.Inbox.EFCore.ModelBuilding;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Stores;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Transactions;
using LucidMicro.Tests.Shared.Persistence;
using LucidMicro.Tests.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class InboxEfCoreTests
{
    [Fact]
    public async Task ConfigureInbox_ConfiguresTableAndColumns()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(InboxMessageEntity));
        Assert.NotNull(entityType);

        var table = StoreObjectIdentifier.Table(InboxModelBuilderExtensions.TableName, null);

        Assert.Equal(InboxModelBuilderExtensions.TableName, entityType.GetTableName());
        Assert.Equal("id", GetColumnName(entityType, nameof(InboxMessageEntity.Id), table));
        Assert.Equal("type", GetColumnName(entityType, nameof(InboxMessageEntity.Type), table));
        Assert.Equal("processed_at", GetColumnName(entityType, nameof(InboxMessageEntity.ProcessedAt), table));
        Assert.Equal("created_at", GetColumnName(entityType, nameof(InboxMessageEntity.CreatedAt), table));
    }

    [Fact]
    public async Task MarkProcessedAsync_AddsInboxMessage()
    {
        var now = DateTimeOffset.Parse("2026-05-27T00:00:00+00:00");
        await using var scope = await CreateContextScopeAsync();
        var store = CreateStore(scope.Context, now);
        var integrationEvent = new TestIntegrationEvent
        {
            Id = Guid.Parse("fe70e234-4ab3-4b64-91af-005d112a0d72")
        };

        await store.MarkProcessedAsync(integrationEvent);
        await store.SaveChangesAsync();

        var storedEntity = await scope.Context.InboxMessages.SingleAsync();
        Assert.Equal(integrationEvent.Id, storedEntity.Id);
        Assert.Equal("test.integration-event.v1", storedEntity.Type);
        Assert.Equal(now, storedEntity.ProcessedAt);
    }

    [Fact]
    public async Task HasProcessedAsync_ReturnsTrue_WhenMessageExists()
    {
        await using var scope = await CreateContextScopeAsync();
        var store = CreateStore(scope.Context, DateTimeOffset.Parse("2026-05-27T00:00:00+00:00"));
        var integrationEvent = new TestIntegrationEvent();

        await store.MarkProcessedAsync(integrationEvent);
        await store.SaveChangesAsync();

        var hasProcessed = await store.HasProcessedAsync(integrationEvent.Id);

        Assert.True(hasProcessed);
    }

    [Fact]
    public void AddLucidEfCoreInbox_RegistersStore()
    {
        var services = new ServiceCollection();

        services.AddLucidEfCoreInbox<TestInboxDbContext>();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInboxMessageStore)
                       && service.ImplementationType == typeof(EfCoreInboxMessageStore<TestInboxDbContext>));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInboxProcessingTransaction)
                       && service.ImplementationType == typeof(EfCoreInboxProcessingTransaction<TestInboxDbContext>));
    }

    [Fact]
    public async Task EfCoreInboxProcessingTransaction_RollsBackChanges_WhenOperationThrows()
    {
        await using var scope = await CreateContextScopeAsync();
        var transaction = new EfCoreInboxProcessingTransaction<TestInboxDbContext>(scope.Context);
        var inboxMessage = InboxMessageEntity.FromMessage(new InboxMessage
        {
            Id = Guid.Parse("78db45ab-c20e-4909-b6a8-8801f85f90f6"),
            Type = "test.integration-event.v1",
            ProcessedAt = DateTimeOffset.Parse("2026-05-27T00:00:00+00:00")
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transaction.ExecuteAsync(
                async _ =>
                {
                    scope.Context.InboxMessages.Add(inboxMessage);
                    await scope.Context.SaveChangesAsync();

                    throw new InvalidOperationException("Failed.");
                }));

        Assert.False(await scope.Context.InboxMessages.AnyAsync());
    }

    private static string? GetColumnName(
        IEntityType entityType,
        string propertyName,
        StoreObjectIdentifier table)
    {
        return entityType.FindProperty(propertyName)?.GetColumnName(table);
    }

    private static Task<SqliteDbContextScope<TestInboxDbContext>> CreateContextScopeAsync()
    {
        return SqliteDbContextScope<TestInboxDbContext>.CreateAsync(connection =>
        {
            var options = new DbContextOptionsBuilder<TestInboxDbContext>()
                .UseSqlite(connection)
                .Options;

            return new TestInboxDbContext(options);
        });
    }

    private static EfCoreInboxMessageStore<TestInboxDbContext> CreateStore(
        TestInboxDbContext dbContext,
        DateTimeOffset now)
    {
        return new EfCoreInboxMessageStore<TestInboxDbContext>(
            dbContext,
            new TestTimeProvider(now));
    }

    [IntegrationEventName("test.integration-event.v1")]
    private sealed record TestIntegrationEvent : IntegrationEvent;

    private sealed class TestInboxDbContext : DbContext
    {
        public TestInboxDbContext(DbContextOptions<TestInboxDbContext> options)
            : base(options)
        {
        }

        public DbSet<InboxMessageEntity> InboxMessages => Set<InboxMessageEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureInbox();
        }
    }
}
