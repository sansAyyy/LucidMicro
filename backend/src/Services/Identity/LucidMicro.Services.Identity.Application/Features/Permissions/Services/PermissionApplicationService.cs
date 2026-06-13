using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.Services.Identity.Application.Features.Permissions.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Permissions.Dtos.Responses;
using LucidMicro.Services.Identity.Application.Features.Permissions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;

namespace LucidMicro.Services.Identity.Application.Features.Permissions.Services;

public sealed class PermissionApplicationService : IPermissionApplicationService
{
    private readonly IReadOnlyRepository<Permission, Guid> _permissions;

    public PermissionApplicationService(IReadOnlyRepository<Permission, Guid> permissions)
    {
        _permissions = permissions;
    }

    public async Task<Result<IReadOnlyList<PermissionResponse>>> GetListAsync(
        CancellationToken cancellationToken = default)
    {
        var permissions = await _permissions.ListAsync(new PermissionsListSpecification(), cancellationToken);
        var responses = permissions.Select(PermissionResponse.FromEntity).ToArray();

        return Result<IReadOnlyList<PermissionResponse>>.Success(responses);
    }
}
