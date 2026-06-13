using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.Services.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleApplicationService _roles;

    public RolesController(IRoleApplicationService roles)
    {
        _roles = roles;
    }

    [HttpGet]
    public async Task<ActionResult<PageResult<RoleResponse>>> GetList(
        [FromQuery] GetRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roles.GetListAsync(request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDetailResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _roles.GetByIdAsync(id, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roles.CreateAsync(request, cancellationToken);

        return this.ToActionResult(
            result,
            role => CreatedAtAction(nameof(GetById), new { id = role.Id }, role));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roles.UpdateAsync(id, request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _roles.DeleteAsync(id, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<ActionResult> AssignPermissions(
        Guid id,
        AssignRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roles.AssignPermissionsAsync(id, request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }
}
