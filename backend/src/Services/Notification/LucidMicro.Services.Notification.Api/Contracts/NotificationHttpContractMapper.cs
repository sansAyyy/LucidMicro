using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Contracts.Notification;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using LucidMicro.Services.Notification.Domain.Enums;
using ContractNotificationResponse = LucidMicro.Contracts.Notification.Http.Responses.NotificationResponse;
using ApplicationNotificationResponse = LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Responses.NotificationResponse;
using SendNotificationRequest = LucidMicro.Contracts.Notification.Http.Requests.SendNotificationRequest;

namespace LucidMicro.Services.Notification.Api.Contracts;

internal static class NotificationHttpContractMapper
{
    public static Result<CreateNotificationRequest> ToApplicationRequest(SendNotificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.TryParse<NotificationChannel>(request.Channel, ignoreCase: true, out var channel)
            || !Enum.IsDefined(channel))
        {
            return Result<CreateNotificationRequest>.Failure(
                Error.Validation("Notification.Validation", "notification channel is invalid."));
        }

        return Result<CreateNotificationRequest>.Success(new CreateNotificationRequest(
            request.Recipient,
            channel,
            request.Subject,
            request.Content));
    }

    public static ContractNotificationResponse ToContract(ApplicationNotificationResponse notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return new ContractNotificationResponse(
            notification.Id,
            notification.Recipient,
            ToContractChannel(notification.Channel),
            notification.Subject,
            notification.Content,
            ToContractStatus(notification.Status),
            notification.SentAt,
            notification.FailedAt,
            notification.FailureReason);
    }

    private static string ToContractChannel(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Sms => NotificationChannels.Sms,
            NotificationChannel.WeChat => NotificationChannels.WeChat,
            NotificationChannel.Email => NotificationChannels.Email,
            NotificationChannel.InApp => NotificationChannels.InApp,
            _ => channel.ToString()
        };
    }

    private static string ToContractStatus(NotificationStatus status)
    {
        return status switch
        {
            NotificationStatus.Pending => NotificationStatuses.Pending,
            NotificationStatus.Sent => NotificationStatuses.Sent,
            NotificationStatus.Failed => NotificationStatuses.Failed,
            _ => status.ToString()
        };
    }
}
