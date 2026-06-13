using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Responses;

namespace LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;

public interface INotificationApplicationService
{
    Task<Result<PageResult<NotificationResponse>>> GetListAsync(
        GetNotificationsRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<NotificationResponse>> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default);
}
