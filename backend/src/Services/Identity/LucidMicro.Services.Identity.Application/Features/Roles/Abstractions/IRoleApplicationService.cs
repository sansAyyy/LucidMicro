using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Responses;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;

public interface IRoleApplicationService
{
    Task<Result<PageResult<RoleResponse>>> GetListAsync(
        GetRolesRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<RoleDetailResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> AssignPermissionsAsync(
        Guid id,
        AssignRolePermissionsRequest request,
        CancellationToken cancellationToken = default);
}
