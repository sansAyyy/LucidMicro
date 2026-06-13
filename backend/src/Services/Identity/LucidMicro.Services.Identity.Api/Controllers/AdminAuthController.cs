using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.Services.Identity.Api.Controllers;

[ApiController]
[Route("api/admin-auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthApplicationService _adminAuthApplicationService;

    public AdminAuthController(IAdminAuthApplicationService adminAuthApplicationService)
    {
        _adminAuthApplicationService = adminAuthApplicationService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginAdminUserResponse>> LoginAsync(
        [FromBody] LoginAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminAuthApplicationService.LoginAsync(request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginAdminUserResponse>> RefreshAsync(
        [FromBody] RefreshAdminUserTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminAuthApplicationService.RefreshAsync(request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentAdminUserResponse>> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        var result = await _adminAuthApplicationService.GetCurrentAsync(cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPut("me/password")]
    [Authorize]
    public async Task<ActionResult> ChangeCurrentPasswordAsync(
        [FromBody] ChangeCurrentAdminUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminAuthApplicationService.ChangeCurrentPasswordAsync(request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }
}
