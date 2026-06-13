using LucidMicro.BuildingBlocks.Outbox.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Outbox.EFCore.ModelBuilding;

public static class OutboxModelBuilderExtensions
{
    public const string TableName = "outbox_messages";

    public static ModelBuilder ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.ToTable(TableName);

            entity.HasKey(message => message.Id);

            entity.Property(message => message.Id)
                .HasColumnName("id");

            entity.Property(message => message.Type)
                .HasColumnName("type")
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(message => message.OccurredAt)
                .HasColumnName("occurred_at")
                .HasPrecision(3)
                .IsRequired();

            entity.Property(message => message.TraceParent)
                .HasColumnName("trace_parent")
                .HasMaxLength(128);

            entity.Property(message => message.TraceState)
                .HasColumnName("trace_state")
                .HasMaxLength(512);

            entity.Property(message => message.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(message => message.CreatedAt)
                .HasColumnName("created_at")
                .HasPrecision(3)
                .IsRequired();

            entity.Property(message => message.PublishedAt)
                .HasColumnName("published_at")
                .HasPrecision(3);

            entity.Property(message => message.LockedUntil)
                .HasColumnName("locked_until")
                .HasPrecision(3);

            entity.Property(message => message.NextRetryAt)
                .HasColumnName("next_retry_at")
                .HasPrecision(3);

            entity.Property(message => message.DeadAt)
                .HasColumnName("dead_at")
                .HasPrecision(3);

            entity.Property(message => message.FailureCount)
                .HasColumnName("failure_count")
                .IsRequired();

            entity.Property(message => message.LastError)
                .HasColumnName("last_error");

            entity.HasIndex(message => message.CreatedAt)
                .HasDatabaseName("ix_outbox_messages_pending")
                .HasFilter("published_at is null and dead_at is null");

            entity.HasIndex(message => message.PublishedAt)
                .HasDatabaseName("ix_outbox_messages_published_at");
        });

        return modelBuilder;
    }
}
