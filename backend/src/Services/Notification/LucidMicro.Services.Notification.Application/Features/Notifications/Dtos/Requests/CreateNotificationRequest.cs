using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;

public sealed record CreateNotificationRequest(
    string? Recipient,
    NotificationChannel Channel,
    string? Subject,
    string? Content);
