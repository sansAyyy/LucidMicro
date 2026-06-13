using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;

public interface IAdminUserApplicationService
{
    Task<Result<PageResult<AdminUserResponse>>> GetListAsync(
        GetAdminUsersRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserResponse>> CreateAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(
        Guid id,
        ResetAdminUserPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> AssignRolesAsync(
        Guid id,
        AssignAdminUserRolesRequest request,
        CancellationToken cancellationToken = default);
}
