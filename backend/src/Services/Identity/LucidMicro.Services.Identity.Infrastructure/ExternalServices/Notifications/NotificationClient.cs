using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Http.Core.Extensions;
using LucidMicro.Contracts.Notification.Http.Requests;
using LucidMicro.Services.Identity.Application.ExternalServices.Notifications;

namespace LucidMicro.Services.Identity.Infrastructure.ExternalServices.Notifications;

public sealed class NotificationClient : INotificationClient
{
    private const string RequestFailedCode = "Identity.Notification.RequestFailed";
    private const string UnavailableCode = "Identity.Notification.Unavailable";
    private readonly HttpClient _httpClient;

    public NotificationClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
    }

    public async Task<Result> SendAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _httpClient.PostAsJsonForResultAsync(
            "internal/notifications",
            request,
            RequestFailedCode,
            UnavailableCode,
            serviceName: "Notification service",
            timeoutMessage: "Notification service request timed out.",
            unavailableMessage: "Notification service is unavailable.",
            cancellationToken);
    }
}
