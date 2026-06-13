using LucidMicro.Services.Notification.Application.Abstractions;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LucidMicro.Services.Notification.Infrastructure.Sending;

public sealed class LogSmsNotificationChannelSender : INotificationChannelSender
{
    private readonly ILogger<LogSmsNotificationChannelSender> _logger;

    public LogSmsNotificationChannelSender(ILogger<LogSmsNotificationChannelSender> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Notification SMS log channel send. MessageId: {MessageId}, Recipient: {Recipient}, Subject: {Subject}",
            message.Id,
            message.Recipient,
            message.Subject);

        return Task.CompletedTask;
    }
}
