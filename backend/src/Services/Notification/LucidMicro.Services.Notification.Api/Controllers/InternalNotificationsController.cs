using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Contracts.Notification.Http.Requests;
using LucidMicro.Services.Notification.Api.Contracts;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using Microsoft.AspNetCore.Mvc;
using ContractNotificationResponse = LucidMicro.Contracts.Notification.Http.Responses.NotificationResponse;

namespace LucidMicro.Services.Notification.Api.Controllers;

[ApiController]
[Route("internal/notifications")]
public sealed class InternalNotificationsController : ControllerBase
{
    private readonly INotificationApplicationService _notifications;

    public InternalNotificationsController(INotificationApplicationService notifications)
    {
        _notifications = notifications;
    }

    [HttpPost]
    public async Task<ActionResult<ContractNotificationResponse>> Create(
        SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var applicationRequest = NotificationHttpContractMapper.ToApplicationRequest(request);
        if (applicationRequest.IsFailure)
        {
            return this.ToActionResult(Result<ContractNotificationResponse>.Failure(applicationRequest.Error));
        }

        var result = await _notifications.CreateAsync(applicationRequest.Value, cancellationToken);
        var contractResult = result.IsSuccess
            ? Result<ContractNotificationResponse>.Success(NotificationHttpContractMapper.ToContract(result.Value))
            : Result<ContractNotificationResponse>.Failure(result.Error);

        return this.ToActionResult(
            contractResult,
            notification => Created($"/api/notifications/{notification.Id}", notification));
    }
}
