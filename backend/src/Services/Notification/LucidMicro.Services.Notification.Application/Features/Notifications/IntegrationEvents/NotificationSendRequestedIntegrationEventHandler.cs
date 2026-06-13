using LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using LucidMicro.Contracts.Notification.IntegrationEvents;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Application.Features.Notifications.IntegrationEvents;

public sealed class NotificationSendRequestedIntegrationEventHandler
    : IIntegrationEventHandler<NotificationSendRequestedIntegrationEvent>
{
    private readonly INotificationApplicationService _notifications;
    private readonly IInboxMessageProcessor _inboxProcessor;

    public NotificationSendRequestedIntegrationEventHandler(
        INotificationApplicationService notifications,
        IInboxMessageProcessor inboxProcessor)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(inboxProcessor);

        _notifications = notifications;
        _inboxProcessor = inboxProcessor;
    }

    public Task HandleAsync(
        NotificationSendRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return _inboxProcessor.ProcessAsync(integrationEvent, ProcessAsync, cancellationToken);
    }

    private async Task ProcessAsync(
        NotificationSendRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NotificationChannel>(
                integrationEvent.Channel,
                ignoreCase: true,
                out var channel))
        {
            throw new ArgumentException(
                $"Unsupported notification channel '{integrationEvent.Channel}'.",
                nameof(integrationEvent));
        }

        var result = await _notifications.CreateAsync(
            new CreateNotificationRequest(
                integrationEvent.Recipient,
                channel,
                integrationEvent.Subject,
                integrationEvent.Content),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to create notification. ErrorCode: {result.Error.Code}, ErrorMessage: {result.Error.Message}");
        }
    }
}
