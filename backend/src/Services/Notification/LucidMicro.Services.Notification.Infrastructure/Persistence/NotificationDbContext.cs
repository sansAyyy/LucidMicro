using LucidMicro.BuildingBlocks.Inbox.EFCore.Entities;
using LucidMicro.BuildingBlocks.Inbox.EFCore.ModelBuilding;
using LucidMicro.BuildingBlocks.Persistence.EFCore.DbContexts;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.Services.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext : LucidDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();

    public DbSet<InboxMessageEntity> InboxMessages => Set<InboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        modelBuilder.ConfigureInbox();

        base.OnModelCreating(modelBuilder);
    }
}
