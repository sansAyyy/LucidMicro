using LucidMicro.BuildingBlocks.Inbox.EFCore.Entities;
using LucidMicro.BuildingBlocks.Inbox.EFCore.ModelBuilding;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;
using LucidMicro.Services.Notification.Infrastructure.Persistence;
using LucidMicro.Tests.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LucidMicro.Services.Notification.Infrastructure.Tests;

public sealed class NotificationDbContextTests
{
    [Fact]
    public async Task ModelCreating_ConfiguresNotificationMessageTableColumnsAndIndexes()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(NotificationMessage));
        Assert.NotNull(entityType);

        var table = StoreObjectIdentifier.Table("notification_messages", null);

        Assert.Equal("notification_messages", entityType.GetTableName());
        Assert.Equal("id", GetColumnName(entityType, nameof(NotificationMessage.Id), table));
        Assert.Equal("recipient", GetColumnName(entityType, nameof(NotificationMessage.Recipient), table));
        Assert.Equal("channel", GetColumnName(entityType, nameof(NotificationMessage.Channel), table));
        Assert.Equal("subject", GetColumnName(entityType, nameof(NotificationMessage.Subject), table));
        Assert.Equal("content", GetColumnName(entityType, nameof(NotificationMessage.Content), table));
        Assert.Equal("status", GetColumnName(entityType, nameof(NotificationMessage.Status), table));
        Assert.Equal("sent_at", GetColumnName(entityType, nameof(NotificationMessage.SentAt), table));
        Assert.Equal("failed_at", GetColumnName(entityType, nameof(NotificationMessage.FailedAt), table));
        Assert.Equal("failure_reason", GetColumnName(entityType, nameof(NotificationMessage.FailureReason), table));

        Assert.Contains(
            entityType.GetIndexes(),
            index => index.GetDatabaseName() == "ix_notification_messages_status");
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.GetDatabaseName() == "ix_notification_messages_created_at");
    }

    [Fact]
    public async Task SaveChanges_PersistsNotificationMessageWithEnumValues()
    {
        await using var scope = await CreateContextScopeAsync();
        var message = NotificationMessage.Create(
            Guid.NewGuid(),
            "admin@example.com",
            NotificationChannel.Email,
            "Welcome",
            "Welcome to LucidMicro.");

        scope.Context.NotificationMessages.Add(message);
        await scope.Context.SaveChangesAsync();

        var storedMessage = await scope.Context.NotificationMessages.SingleAsync();
        Assert.Equal(NotificationChannel.Email, storedMessage.Channel);
        Assert.Equal(NotificationStatus.Pending, storedMessage.Status);
    }

    [Fact]
    public async Task ModelCreating_ConfiguresInboxMessageTableColumnsAndIndexes()
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
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.GetDatabaseName() == "ix_inbox_messages_processed_at");
    }

    private static string? GetColumnName(
        IEntityType entityType,
        string propertyName,
        StoreObjectIdentifier table)
    {
        return entityType.FindProperty(propertyName)?.GetColumnName(table);
    }

    private static Task<SqliteDbContextScope<NotificationDbContext>> CreateContextScopeAsync()
    {
        return SqliteDbContextScope<NotificationDbContext>.CreateAsync(connection =>
        {
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseSqlite(connection)
                .Options;

            return new NotificationDbContext(options);
        });
    }
}
