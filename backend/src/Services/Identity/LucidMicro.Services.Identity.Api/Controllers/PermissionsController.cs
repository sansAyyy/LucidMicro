using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.Services.Identity.Application.Features.Permissions.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Permissions.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.Services.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionApplicationService _permissions;

    public PermissionsController(IPermissionApplicationService permissions)
    {
        _permissions = permissions;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> GetList(
        CancellationToken cancellationToken)
    {
        var result = await _permissions.GetListAsync(cancellationToken);

        return this.ToActionResult(result);
    }
}
