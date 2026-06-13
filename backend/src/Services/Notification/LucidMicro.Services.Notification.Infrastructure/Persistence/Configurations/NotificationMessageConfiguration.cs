using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.Services.Notification.Infrastructure.Persistence.Configurations;

public sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("notification_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id");

        builder.Property(message => message.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(message => message.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(message => message.Subject)
            .HasColumnName("subject")
            .HasMaxLength(256);

        builder.Property(message => message.Content)
            .HasColumnName("content")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(message => message.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(message => message.SentAt)
            .HasColumnName("sent_at")
            .HasPrecision(3);

        builder.Property(message => message.FailedAt)
            .HasColumnName("failed_at")
            .HasPrecision(3);

        builder.Property(message => message.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(2000);

        builder.HasIndex(message => message.Status)
            .HasDatabaseName("ix_notification_messages_status");

        builder.HasIndex(message => message.CreatedAt)
            .HasDatabaseName("ix_notification_messages_created_at");
    }
}
