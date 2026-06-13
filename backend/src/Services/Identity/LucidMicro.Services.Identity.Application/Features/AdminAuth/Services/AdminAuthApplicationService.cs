using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Responses;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Errors;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Services;

public sealed class AdminAuthApplicationService : IAdminAuthApplicationService
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly IReadOnlyAdminUserPermissionRepository _adminUserPermissionRepository;
    private readonly IRepository<AdminUser, Guid> _adminUsers;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenValidator _refreshTokenValidator;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ChangeCurrentAdminUserPasswordRequest> _changePasswordValidator;
    private readonly IValidator<RefreshAdminUserTokenRequest> _refreshValidator;
    private readonly IValidator<LoginAdminUserRequest> _validator;

    public AdminAuthApplicationService(
        IRepository<AdminUser, Guid> adminUsers,
        IReadOnlyAdminUserPermissionRepository adminUserPermissionRepository,
        IUnitOfWork unitOfWork,
        IPasswordHashingService passwordHashingService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenValidator refreshTokenValidator,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IValidator<ChangeCurrentAdminUserPasswordRequest> changePasswordValidator,
        IValidator<RefreshAdminUserTokenRequest> refreshValidator,
        IValidator<LoginAdminUserRequest> validator)
    {
        _adminUsers = adminUsers;
        _adminUserPermissionRepository = adminUserPermissionRepository;
        _unitOfWork = unitOfWork;
        _passwordHashingService = passwordHashingService;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _refreshTokenValidator = refreshTokenValidator;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _changePasswordValidator = changePasswordValidator;
        _refreshValidator = refreshValidator;
        _validator = validator;
    }

    public async Task<Result<LoginAdminUserResponse>> LoginAsync(
        LoginAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<LoginAdminUserResponse>.Failure(
                validationResult.ToValidationError(AdminAuthErrors.ValidationErrorCode));
        }

        var loginName = request.LoginName!.Trim();
        var adminUser = await _adminUsers.FirstOrDefaultAsync(
            new AdminUserByLoginNameSpecification(loginName),
            cancellationToken);

        if (adminUser is null)
        {
            return Result<LoginAdminUserResponse>.Failure(AdminAuthErrors.InvalidCredentials());
        }

        if (!adminUser.IsActive)
        {
            return Result<LoginAdminUserResponse>.Failure(AdminAuthErrors.Disabled());
        }

        var passwordVerificationResult = _passwordHashingService.VerifyHashedPassword(
            adminUser.PasswordHash,
            request.Password!);

        if (passwordVerificationResult == PasswordHashVerificationResult.Failed)
        {
            return Result<LoginAdminUserResponse>.Failure(AdminAuthErrors.InvalidCredentials());
        }

        if (passwordVerificationResult == PasswordHashVerificationResult.SuccessRehashNeeded)
        {
            adminUser.ChangePassword(_passwordHashingService.HashPassword(request.Password!));
        }

        adminUser.MarkLogin(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginAdminUserResponse>.Success(CreateLoginResponse(adminUser));
    }

    public async Task<Result<LoginAdminUserResponse>> RefreshAsync(
        RefreshAdminUserTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _refreshValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<LoginAdminUserResponse>.Failure(
                validationResult.ToValidationError(AdminAuthErrors.ValidationErrorCode));
        }

        var tokenClaims = _refreshTokenValidator.ValidateRefreshToken(request.RefreshToken!.Trim());
        if (tokenClaims is null
            || !Guid.TryParse(tokenClaims.Subject, out var adminUserId))
        {
            return Result<LoginAdminUserResponse>.Failure(AdminAuthErrors.InvalidRefreshToken());
        }

        var adminUser = await _adminUsers.GetByIdAsync(adminUserId, cancellationToken);
        if (adminUser is null)
        {
            return Result<LoginAdminUserResponse>.Failure(AdminAuthErrors.InvalidRefreshToken());
        }

        if (!adminUser.IsActive)
        {
            return Result<LoginAdminUserResponse>.Failure(AdminAuthErrors.Disabled());
        }

        return Result<LoginAdminUserResponse>.Success(CreateLoginResponse(adminUser));
    }

    public async Task<Result<CurrentAdminUserResponse>> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var adminUserResult = await GetCurrentAdminUserAsync(cancellationToken);
        if (adminUserResult.IsFailure)
        {
            return Result<CurrentAdminUserResponse>.Failure(
                adminUserResult.Error);
        }

        var permissions = await _adminUserPermissionRepository.GetPermissionCodesAsync(
            adminUserResult.Value.Id,
            cancellationToken);

        return Result<CurrentAdminUserResponse>.Success(
            CurrentAdminUserResponse.FromEntity(adminUserResult.Value, permissions));
    }

    public async Task<Result> ChangeCurrentPasswordAsync(
        ChangeCurrentAdminUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.ToValidationError(AdminAuthErrors.ValidationErrorCode));
        }

        var adminUserResult = await GetCurrentAdminUserAsync(cancellationToken);
        if (adminUserResult.IsFailure)
        {
            return Result.Failure(adminUserResult.Error);
        }

        var adminUser = adminUserResult.Value;
        var passwordVerificationResult = _passwordHashingService.VerifyHashedPassword(
            adminUser.PasswordHash,
            request.CurrentPassword!);

        if (passwordVerificationResult == PasswordHashVerificationResult.Failed)
        {
            return Result.Failure(AdminAuthErrors.InvalidCredentials());
        }

        adminUser.ChangePassword(_passwordHashingService.HashPassword(request.NewPassword!));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result<AdminUser>> GetCurrentAdminUserAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || !Guid.TryParse(_currentUser.UserId, out var adminUserId))
        {
            return Result<AdminUser>.Failure(AdminAuthErrors.InvalidCurrentUser());
        }

        var adminUser = await _adminUsers.GetByIdAsync(adminUserId, cancellationToken);
        if (adminUser is null)
        {
            return Result<AdminUser>.Failure(AdminAuthErrors.InvalidCurrentUser());
        }

        if (!adminUser.IsActive)
        {
            return Result<AdminUser>.Failure(AdminAuthErrors.Disabled());
        }

        return Result<AdminUser>.Success(adminUser);
    }

    private LoginAdminUserResponse CreateLoginResponse(AdminUser adminUser)
    {
        var tokenClaims = new AccessTokenClaims(
            adminUser.Id.ToString(),
            adminUser.UserName,
            new Dictionary<string, string>
            {
                ["email"] = adminUser.Email
            });
        var accessToken = _accessTokenService.GenerateAccessToken(tokenClaims);
        var refreshToken = _refreshTokenService.GenerateRefreshToken(tokenClaims);

        return new LoginAdminUserResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt);
    }
}
