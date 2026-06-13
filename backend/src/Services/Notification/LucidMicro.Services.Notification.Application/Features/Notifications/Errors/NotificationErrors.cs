using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.Services.Notification.Application.Features.Notifications.Errors;

public static class NotificationErrors
{
    public static Error InvalidChannel()
    {
        return Error.Validation("Notification.Validation", "notification channel is invalid.");
    }

    public static Error NotFound(Guid id)
    {
        return Error.NotFound("Notification.NotFound", $"Notification '{id}' was not found.");
    }
}
