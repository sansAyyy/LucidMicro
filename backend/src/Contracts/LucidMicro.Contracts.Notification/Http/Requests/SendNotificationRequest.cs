namespace LucidMicro.Contracts.Notification.Http.Requests;

public sealed record SendNotificationRequest(
    string? Recipient,
    string? Channel,
    string? Subject,
    string? Content);
