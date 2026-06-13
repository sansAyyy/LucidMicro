using LucidMicro.Services.Notification.Application.Abstractions;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LucidMicro.Services.Notification.Infrastructure.Sending;

public sealed class DefaultNotificationSender : INotificationSender
{
    private readonly IReadOnlyDictionary<NotificationChannel, INotificationChannelSender> _senders;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultNotificationSender> _logger;

    public DefaultNotificationSender(
        IEnumerable<INotificationChannelSender> senders,
        TimeProvider timeProvider,
        ILogger<DefaultNotificationSender> logger)
    {
        ArgumentNullException.ThrowIfNull(senders);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _senders = senders.ToDictionary(sender => sender.Channel);
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            if (!_senders.TryGetValue(message.Channel, out var sender))
            {
                throw new InvalidOperationException(
                    $"Notification channel sender is not registered: {message.Channel}.");
            }

            await sender.SendAsync(message, cancellationToken);
            message.MarkSent(_timeProvider.GetUtcNow());

            _logger.LogInformation(
                "Notification message sent. MessageId: {MessageId}, Channel: {Channel}",
                message.Id,
                message.Channel);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            message.MarkFailed(_timeProvider.GetUtcNow(), GetErrorMessage(exception));

            _logger.LogError(
                exception,
                "Notification message send failed. MessageId: {MessageId}, Channel: {Channel}",
                message.Id,
                message.Channel);
        }
    }

    private static string GetErrorMessage(Exception exception)
    {
        return string.IsNullOrWhiteSpace(exception.Message)
            ? exception.GetType().Name
            : exception.Message;
    }
}
