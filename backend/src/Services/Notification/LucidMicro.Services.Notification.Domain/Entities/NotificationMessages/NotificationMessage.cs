using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Domain.Core.Guards;
using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;

public class NotificationMessage : AuditableEntity<Guid>
{
    private NotificationMessage()
    {
    }

    private NotificationMessage(
        Guid id,
        string recipient,
        NotificationChannel channel,
        string? subject,
        string content)
    {
        Id = id;
        Recipient = DomainGuard.RequiredText(recipient, nameof(recipient), 256);
        Channel = channel;
        Subject = DomainGuard.OptionalText(subject, nameof(subject), 256);
        Content = DomainGuard.RequiredText(content, nameof(content), 4000);
        Status = NotificationStatus.Pending;
    }

    public string Recipient { get; private set; } = string.Empty;

    public NotificationChannel Channel { get; private set; }

    public string? Subject { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public NotificationStatus Status { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public static NotificationMessage Create(
        Guid id,
        string recipient,
        NotificationChannel channel,
        string? subject,
        string content)
    {
        return new NotificationMessage(id, recipient, channel, subject, content);
    }

    public void MarkSent(DateTimeOffset sentAt)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAt;
        FailedAt = null;
        FailureReason = null;
    }

    public void MarkFailed(DateTimeOffset failedAt, string failureReason)
    {
        Status = NotificationStatus.Failed;
        SentAt = null;
        FailedAt = failedAt;
        FailureReason = DomainGuard.RequiredText(failureReason, nameof(failureReason), 2000);
    }
}
