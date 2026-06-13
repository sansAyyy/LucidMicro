using LucidMicro.BuildingBlocks.Inbox.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Inbox.EFCore.ModelBuilding;

public static class InboxModelBuilderExtensions
{
    public const string TableName = "inbox_messages";

    public static ModelBuilder ConfigureInbox(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<InboxMessageEntity>(entity =>
        {
            entity.ToTable(TableName);

            entity.HasKey(message => message.Id);

            entity.Property(message => message.Id)
                .HasColumnName("id");

            entity.Property(message => message.Type)
                .HasColumnName("type")
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(message => message.ProcessedAt)
                .HasColumnName("processed_at")
                .HasPrecision(3)
                .IsRequired();

            entity.Property(message => message.CreatedAt)
                .HasColumnName("created_at")
                .HasPrecision(3)
                .IsRequired();

            entity.HasIndex(message => message.Type)
                .HasDatabaseName("ix_inbox_messages_type");

            entity.HasIndex(message => message.ProcessedAt)
                .HasDatabaseName("ix_inbox_messages_processed_at");
        });

        return modelBuilder;
    }
}
