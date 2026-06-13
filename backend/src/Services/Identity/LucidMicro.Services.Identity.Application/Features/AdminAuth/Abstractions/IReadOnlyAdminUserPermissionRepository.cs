namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;

public interface IReadOnlyAdminUserPermissionRepository
{
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default);
}
