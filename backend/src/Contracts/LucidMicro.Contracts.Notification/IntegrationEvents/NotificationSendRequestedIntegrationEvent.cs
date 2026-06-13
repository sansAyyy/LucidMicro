using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

namespace LucidMicro.Contracts.Notification.IntegrationEvents;

[IntegrationEventName("notification.send-requested.v1")]
public sealed record NotificationSendRequestedIntegrationEvent : IntegrationEvent
{
    public required string Recipient { get; init; }

    public required string Channel { get; init; }

    public string? Subject { get; init; }

    public required string Content { get; init; }

    public static NotificationSendRequestedIntegrationEvent Create(
        string recipient,
        string channel,
        string? subject,
        string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new NotificationSendRequestedIntegrationEvent
        {
            Recipient = recipient.Trim(),
            Channel = channel.Trim(),
            Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim(),
            Content = content.Trim()
        };
    }
}
