using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Responses;

public sealed record NotificationResponse(
    Guid Id,
    string Recipient,
    NotificationChannel Channel,
    string? Subject,
    string Content,
    NotificationStatus Status,
    DateTimeOffset? SentAt,
    DateTimeOffset? FailedAt,
    string? FailureReason)
{
    public static NotificationResponse FromEntity(NotificationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new NotificationResponse(
            message.Id,
            message.Recipient,
            message.Channel,
            message.Subject,
            message.Content,
            message.Status,
            message.SentAt,
            message.FailedAt,
            message.FailureReason);
    }
}
