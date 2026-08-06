using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Authorization;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Notification.Api.Contracts;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using LucidMicro.Services.Notification.Application.Authorization;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractNotificationResponse = LucidMicro.Contracts.Notification.Http.Responses.NotificationResponse;

namespace LucidMicro.Services.Notification.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationApplicationService _notifications;

    public NotificationsController(INotificationApplicationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    [RequirePermission(NotificationPermissions.NotificationsRead)]
    public async Task<ActionResult<PageResult<ContractNotificationResponse>>> GetList(
        [FromQuery] GetNotificationsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _notifications.GetListAsync(request, cancellationToken);
        var contractResult = result.IsSuccess
            ? Result<PageResult<ContractNotificationResponse>>.Success(
                result.Value.Map(NotificationHttpContractMapper.ToContract))
            : Result<PageResult<ContractNotificationResponse>>.Failure(result.Error);

        return this.ToActionResult(contractResult);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(NotificationPermissions.NotificationsRead)]
    public async Task<ActionResult<ContractNotificationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _notifications.GetByIdAsync(id, cancellationToken);
        var contractResult = result.IsSuccess
            ? Result<ContractNotificationResponse>.Success(NotificationHttpContractMapper.ToContract(result.Value))
            : Result<ContractNotificationResponse>.Failure(result.Error);

        return this.ToActionResult(contractResult);
    }
}
