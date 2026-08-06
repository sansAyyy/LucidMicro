using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Authorization;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Identity.Application.Authorization;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.Services.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin-users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserApplicationService _adminUsers;

    public AdminUsersController(IAdminUserApplicationService adminUsers)
    {
        _adminUsers = adminUsers;
    }

    [HttpGet]
    [RequirePermission(IdentityPermissions.AdminUsersRead)]
    public async Task<ActionResult<PageResult<AdminUserResponse>>> GetList(
        [FromQuery] GetAdminUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.GetListAsync(request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(IdentityPermissions.AdminUsersRead)]
    public async Task<ActionResult<AdminUserResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.GetByIdAsync(id, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost]
    [RequirePermission(IdentityPermissions.AdminUsersCreate)]
    public async Task<ActionResult<AdminUserResponse>> Create(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.CreateAsync(request, cancellationToken);

        return this.ToActionResult(
            result,
            adminUser => CreatedAtAction(nameof(GetById), new { id = adminUser.Id }, adminUser));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(IdentityPermissions.AdminUsersUpdate)]
    public async Task<ActionResult> Update(
        Guid id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.UpdateAsync(id, request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(IdentityPermissions.AdminUsersDelete)]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.DeleteAsync(id, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpPut("{id:guid}/activate")]
    [RequirePermission(IdentityPermissions.AdminUsersEnable)]
    public async Task<ActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.ActivateAsync(id, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpPut("{id:guid}/deactivate")]
    [RequirePermission(IdentityPermissions.AdminUsersDisable)]
    public async Task<ActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.DeactivateAsync(id, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpPut("{id:guid}/password")]
    [RequirePermission(IdentityPermissions.AdminUsersResetPassword)]
    public async Task<ActionResult> ResetPassword(
        Guid id,
        ResetAdminUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.ResetPasswordAsync(id, request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpPut("{id:guid}/roles")]
    [RequirePermission(IdentityPermissions.AdminUsersUpdate)]
    public async Task<ActionResult> AssignRoles(
        Guid id,
        AssignAdminUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminUsers.AssignRolesAsync(id, request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }
}
