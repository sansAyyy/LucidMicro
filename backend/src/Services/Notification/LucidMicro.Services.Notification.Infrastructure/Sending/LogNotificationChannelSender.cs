using LucidMicro.Services.Notification.Application.Abstractions;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LucidMicro.Services.Notification.Infrastructure.Sending;

public sealed class LogNotificationChannelSender : INotificationChannelSender
{
    private readonly ILogger<LogNotificationChannelSender> _logger;

    public LogNotificationChannelSender(ILogger<LogNotificationChannelSender> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Notification log channel send. MessageId: {MessageId}, Recipient: {Recipient}, Subject: {Subject}",
            message.Id,
            message.Recipient,
            message.Subject);

        return Task.CompletedTask;
    }
}
