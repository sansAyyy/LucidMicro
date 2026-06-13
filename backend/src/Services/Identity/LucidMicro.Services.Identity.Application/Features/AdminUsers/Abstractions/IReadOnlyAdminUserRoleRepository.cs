namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;

using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;

public interface IReadOnlyAdminUserRoleRepository
{
    Task<IReadOnlyList<AdminUserRoleResponse>> GetRolesAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default);
}
