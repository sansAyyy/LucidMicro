namespace LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;

public sealed record GetNotificationsRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Channel { get; init; }

    public string? Keyword { get; init; }

    public DateOnly? SentFrom { get; init; }

    public DateOnly? SentTo { get; init; }
}
