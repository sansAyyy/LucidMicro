namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;

public sealed record AssignAdminUserRolesRequest
{
    public IReadOnlyList<Guid> RoleIds { get; init; } = [];
}
