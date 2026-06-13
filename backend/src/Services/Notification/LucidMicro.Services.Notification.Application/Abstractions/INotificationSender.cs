using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;

namespace LucidMicro.Services.Notification.Application.Abstractions;

public interface INotificationSender
{
    Task SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default);
}
