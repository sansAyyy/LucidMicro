namespace LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;

public interface IReadOnlyRolePermissionRepository
{
    Task<IReadOnlyList<Guid>> GetPermissionIdsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}
