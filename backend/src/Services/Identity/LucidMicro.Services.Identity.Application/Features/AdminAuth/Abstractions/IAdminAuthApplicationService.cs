using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Responses;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;

public interface IAdminAuthApplicationService
{
    Task<Result<LoginAdminUserResponse>> LoginAsync(
        LoginAdminUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LoginAdminUserResponse>> RefreshAsync(
        RefreshAdminUserTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentAdminUserResponse>> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<Result> ChangeCurrentPasswordAsync(
        ChangeCurrentAdminUserPasswordRequest request,
        CancellationToken cancellationToken = default);
}
