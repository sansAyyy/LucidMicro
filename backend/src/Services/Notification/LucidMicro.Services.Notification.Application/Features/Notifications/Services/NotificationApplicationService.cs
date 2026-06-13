using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Notification.Application.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Responses;
using LucidMicro.Services.Notification.Application.Features.Notifications.Errors;
using LucidMicro.Services.Notification.Application.Features.Notifications.Specifications;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Application.Features.Notifications.Services;

public sealed class NotificationApplicationService : INotificationApplicationService
{
    private readonly IRepository<NotificationMessage, Guid> _notifications;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationApplicationService(
        IRepository<NotificationMessage, Guid> notifications,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(notificationSender);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _notifications = notifications;
        _notificationSender = notificationSender;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PageResult<NotificationResponse>>> GetListAsync(
        GetNotificationsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageRequest = new PageRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        if (!TryParseChannel(request.Channel, out var channel))
        {
            return Result<PageResult<NotificationResponse>>.Failure(NotificationErrors.InvalidChannel());
        }

        var notifications = await _notifications.PageAsync(
            new NotificationsListSpecification(
                channel,
                request.Keyword,
                request.SentFrom,
                request.SentTo),
            pageRequest,
            cancellationToken);

        return Result<PageResult<NotificationResponse>>.Success(
            notifications.Map(NotificationResponse.FromEntity));
    }

    public async Task<Result<NotificationResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var message = await _notifications.GetByIdAsync(id, cancellationToken);
        if (message is null)
        {
            return Result<NotificationResponse>.Failure(NotificationErrors.NotFound(id));
        }

        return Result<NotificationResponse>.Success(NotificationResponse.FromEntity(message));
    }

    private static bool TryParseChannel(
        string? channel,
        out NotificationChannel? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(channel))
        {
            return true;
        }

        if (!Enum.TryParse<NotificationChannel>(channel.Trim(), ignoreCase: true, out var parsedValue)
            || !Enum.IsDefined(parsedValue))
        {
            return false;
        }

        value = parsedValue;
        return true;
    }

    public async Task<Result<NotificationResponse>> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = NotificationMessage.Create(
            Guid.NewGuid(),
            request.Recipient ?? string.Empty,
            request.Channel,
            request.Subject,
            request.Content ?? string.Empty);

        await _notifications.AddAsync(message, cancellationToken);
        await _notificationSender.SendAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<NotificationResponse>.Success(NotificationResponse.FromEntity(message));
    }
}
