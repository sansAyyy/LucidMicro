using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Application.Features.Notifications.Specifications;

internal sealed class NotificationsListSpecification : Specification<NotificationMessage>
{
    public NotificationsListSpecification(
        NotificationChannel? channel,
        string? keyword,
        DateOnly? sentFrom,
        DateOnly? sentTo)
    {
        var normalizedKeyword = keyword?.Trim();
        var hasKeyword = !string.IsNullOrWhiteSpace(normalizedKeyword);
        var sentFromAt = sentFrom.HasValue
            ? ToUtcStart(sentFrom.Value)
            : (DateTimeOffset?)null;
        var sentToExclusive = sentTo.HasValue
            ? ToUtcStart(sentTo.Value.AddDays(1))
            : (DateTimeOffset?)null;

        Where(notification =>
            (!channel.HasValue || notification.Channel == channel.Value)
            && (!hasKeyword
                || notification.Recipient.Contains(normalizedKeyword!)
                || (notification.Subject != null && notification.Subject.Contains(normalizedKeyword!))
                || notification.Content.Contains(normalizedKeyword!)
                || (notification.FailureReason != null && notification.FailureReason.Contains(normalizedKeyword!)))
            && (!sentFromAt.HasValue || (notification.SentAt ?? notification.FailedAt) >= sentFromAt.Value)
            && (!sentToExclusive.HasValue || (notification.SentAt ?? notification.FailedAt) < sentToExclusive.Value));

        OrderByDescending(notification => notification.CreatedAt);
        ApplyNoTracking();
    }

    private static DateTimeOffset ToUtcStart(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
