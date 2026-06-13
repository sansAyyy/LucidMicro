using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Contracts.Notification.Http.Requests;

namespace LucidMicro.Services.Identity.Application.ExternalServices.Notifications;

public interface INotificationClient
{
    Task<Result> SendAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken = default);
}
