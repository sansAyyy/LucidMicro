using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Services.Identity.Application.Features.Permissions.Dtos.Responses;

namespace LucidMicro.Services.Identity.Application.Features.Permissions.Abstractions;

public interface IPermissionApplicationService
{
    Task<Result<IReadOnlyList<PermissionResponse>>> GetListAsync(
        CancellationToken cancellationToken = default);
}
