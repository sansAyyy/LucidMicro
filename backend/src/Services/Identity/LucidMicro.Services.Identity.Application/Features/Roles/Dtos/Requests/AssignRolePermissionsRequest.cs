namespace LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;

public sealed record AssignRolePermissionsRequest
{
    public IReadOnlyList<Guid> PermissionIds { get; init; } = [];
}
