using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Application.Abstractions;

public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }

    Task SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default);
}
