namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;

public interface IAdminUserRoleRepository : IReadOnlyAdminUserRoleRepository
{
    Task ReplaceRolesAsync(
        Guid adminUserId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default);
}
