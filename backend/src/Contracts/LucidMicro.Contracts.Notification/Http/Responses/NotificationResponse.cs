namespace LucidMicro.Contracts.Notification.Http.Responses;

public sealed record NotificationResponse(
    Guid Id,
    string Recipient,
    string Channel,
    string? Subject,
    string Content,
    string Status,
    DateTimeOffset? SentAt,
    DateTimeOffset? FailedAt,
    string? FailureReason);
