using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Services;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Validators;
using LucidMicro.Services.Identity.Application.Tests.TestDoubles;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Tests.Shared.Time;

namespace LucidMicro.Services.Identity.Application.Tests;

public sealed class AdminAuthApplicationServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsValidationError_WhenRequestIsInvalid()
    {
        var context = CreateContext();

        var result = await context.Service.LoginAsync(new LoginAdminUserRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.Validation", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task LoginAsync_ReturnsUnauthorized_WhenAdminUserDoesNotExist()
    {
        var context = CreateContext();

        var result = await context.Service.LoginAsync(new LoginAdminUserRequest
        {
            LoginName = "missing",
            Password = "secret"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidCredentials", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task LoginAsync_ReturnsForbidden_WhenAdminUserIsDisabled()
    {
        var adminUser = CreateAdminUser(isActive: false);
        var context = CreateContext(adminUser);

        var result = await context.Service.LoginAsync(new LoginAdminUserRequest
        {
            LoginName = adminUser.UserName,
            Password = "secret"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.Disabled", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task LoginAsync_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.LoginAsync(new LoginAdminUserRequest
        {
            LoginName = adminUser.UserName,
            Password = "wrong"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidCredentials", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokensAndMarksLogin_WhenCredentialsAreValid()
    {
        var now = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser, now);
        context.AdminUserPermissionRepository.SetPermissions(
            adminUser.Id,
            "identity.admin-users.read",
            "identity.roles.read");

        var result = await context.Service.LoginAsync(new LoginAdminUserRequest
        {
            LoginName = " admin ",
            Password = "secret"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal($"token:{adminUser.Id}", result.Value.AccessToken);
        Assert.Equal(new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero), result.Value.ExpiresAt);
        Assert.Equal($"refresh-token:{adminUser.Id}", result.Value.RefreshToken);
        Assert.Equal(new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero), result.Value.RefreshTokenExpiresAt);
        Assert.Equal(now.UtcDateTime, adminUser.LastLoginAt);
        Assert.Equal("hashed:secret", adminUser.PasswordHash);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
        Assert.NotNull(context.AccessTokenService.LastClaims);
        Assert.Equal(adminUser.Id.ToString(), context.AccessTokenService.LastClaims.Subject);
        Assert.Equal(adminUser.UserName, context.AccessTokenService.LastClaims.Name);
        Assert.Equal(adminUser.Email, context.AccessTokenService.LastClaims.AdditionalClaims?["email"]);
        Assert.Equal("1", context.AccessTokenService.LastClaims.AdditionalClaims?["auth_ver"]);
        Assert.Equal(
            ["identity.admin-users.read", "identity.roles.read"],
            context.AccessTokenService.LastClaims.AdditionalClaimValues
                .Where(claim => claim.Type == "permission")
                .Select(claim => claim.Value)
                .ToArray());
        Assert.NotNull(context.AccessTokenService.LastRefreshClaims);
        Assert.Equal(adminUser.Id.ToString(), context.AccessTokenService.LastRefreshClaims.Subject);
        Assert.Equal(adminUser.UserName, context.AccessTokenService.LastRefreshClaims.Name);
        Assert.Equal(adminUser.Email, context.AccessTokenService.LastRefreshClaims.AdditionalClaims?["email"]);
    }

    [Fact]
    public async Task LoginAsync_CanUseEmailAsLoginName()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.LoginAsync(new LoginAdminUserRequest
        {
            LoginName = adminUser.Email,
            Password = "secret"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal($"token:{adminUser.Id}", result.Value.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_RehashesPassword_WhenVerificationRequiresRehash()
    {
        var adminUser = CreateAdminUser(passwordHash: "legacy:secret");
        var context = CreateContext(adminUser);

        var result = await context.Service.LoginAsync(new LoginAdminUserRequest
        {
            LoginName = adminUser.UserName,
            Password = "secret"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed:secret", adminUser.PasswordHash);
        Assert.Equal(1, context.PasswordHashingService.HashCount);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsValidationError_WhenRequestIsInvalid()
    {
        var context = CreateContext();

        var result = await context.Service.RefreshAsync(new RefreshAdminUserTokenRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.Validation", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsUnauthorized_WhenRefreshTokenIsInvalid()
    {
        var context = CreateContext();

        var result = await context.Service.RefreshAsync(new RefreshAdminUserTokenRequest
        {
            RefreshToken = "invalid"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal("invalid", context.AccessTokenService.LastValidatedRefreshToken);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsUnauthorized_WhenRefreshTokenSubjectIsInvalid()
    {
        var context = CreateContext();
        context.AccessTokenService.ValidatedRefreshTokenClaims = new AccessTokenClaims("invalid");

        var result = await context.Service.RefreshAsync(new RefreshAdminUserTokenRequest
        {
            RefreshToken = "refresh-token"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsUnauthorized_WhenAdminUserDoesNotExist()
    {
        var context = CreateContext();
        context.AccessTokenService.ValidatedRefreshTokenClaims = new AccessTokenClaims(Guid.NewGuid().ToString());

        var result = await context.Service.RefreshAsync(new RefreshAdminUserTokenRequest
        {
            RefreshToken = "refresh-token"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsForbidden_WhenAdminUserIsDisabled()
    {
        var adminUser = CreateAdminUser(isActive: false);
        var context = CreateContext(adminUser);
        context.AccessTokenService.ValidatedRefreshTokenClaims = new AccessTokenClaims(adminUser.Id.ToString());

        var result = await context.Service.RefreshAsync(new RefreshAdminUserTokenRequest
        {
            RefreshToken = "refresh-token"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.Disabled", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsTokens_WhenRefreshTokenIsValid()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);
        context.AccessTokenService.ValidatedRefreshTokenClaims = new AccessTokenClaims(adminUser.Id.ToString());

        var result = await context.Service.RefreshAsync(new RefreshAdminUserTokenRequest
        {
            RefreshToken = " refresh-token "
        });

        Assert.True(result.IsSuccess);
        Assert.Equal($"token:{adminUser.Id}", result.Value.AccessToken);
        Assert.Equal($"refresh-token:{adminUser.Id}", result.Value.RefreshToken);
        Assert.Equal("refresh-token", context.AccessTokenService.LastValidatedRefreshToken);
        Assert.Null(adminUser.LastLoginAt);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
        Assert.NotNull(context.AccessTokenService.LastClaims);
        Assert.Equal(adminUser.Id.ToString(), context.AccessTokenService.LastClaims.Subject);
        Assert.NotNull(context.AccessTokenService.LastRefreshClaims);
        Assert.Equal(adminUser.Id.ToString(), context.AccessTokenService.LastRefreshClaims.Subject);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        var context = CreateContext();

        var result = await context.Service.GetCurrentAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidCurrentUser", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsUnauthorized_WhenCurrentUserIdIsInvalid()
    {
        var context = CreateContext();
        context.CurrentUser.IsAuthenticated = true;
        context.CurrentUser.UserId = "invalid";

        var result = await context.Service.GetCurrentAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidCurrentUser", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsUnauthorized_WhenAdminUserDoesNotExist()
    {
        var context = CreateContext();
        context.CurrentUser.IsAuthenticated = true;
        context.CurrentUser.UserId = Guid.NewGuid().ToString();

        var result = await context.Service.GetCurrentAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidCurrentUser", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsForbidden_WhenAdminUserIsDisabled()
    {
        var adminUser = CreateAdminUser(isActive: false);
        var context = CreateContext(adminUser);
        context.CurrentUser.IsAuthenticated = true;
        context.CurrentUser.UserId = adminUser.Id.ToString();

        var result = await context.Service.GetCurrentAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.Disabled", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsCurrentAdminUser_WhenCurrentUserIsValid()
    {
        var adminUser = CreateAdminUser();
        adminUser.MarkLogin(new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc));
        var context = CreateContext(adminUser);
        context.CurrentUser.IsAuthenticated = true;
        context.CurrentUser.UserId = adminUser.Id.ToString();
        context.AdminUserPermissionRepository.SetPermissions(
            adminUser.Id,
            "identity.admin-users.read",
            "identity.roles.read");

        var result = await context.Service.GetCurrentAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(adminUser.Id, result.Value.Id);
        Assert.Equal(adminUser.UserName, result.Value.UserName);
        Assert.Equal(adminUser.Email, result.Value.Email);
        Assert.Equal(adminUser.DisplayName, result.Value.DisplayName);
        Assert.Equal(adminUser.PhoneNumber, result.Value.PhoneNumber);
        Assert.Equal(adminUser.IsActive, result.Value.IsActive);
        Assert.Equal(adminUser.LastLoginAt, result.Value.LastLoginAt);
        Assert.Equal(["identity.admin-users.read", "identity.roles.read"], result.Value.Permissions);
        Assert.Equal(1, context.AdminUserPermissionRepository.GetPermissionCodesCount);
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_ReturnsValidationError_WhenRequestIsInvalid()
    {
        var context = CreateContext();

        var result = await context.Service.ChangeCurrentPasswordAsync(new ChangeCurrentAdminUserPasswordRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.Validation", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_ReturnsValidationError_WhenConfirmPasswordDoesNotMatch()
    {
        var context = CreateContext();

        var result = await context.Service.ChangeCurrentPasswordAsync(new ChangeCurrentAdminUserPasswordRequest
        {
            CurrentPassword = "secret",
            NewPassword = "new-secret",
            ConfirmPassword = "different"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("confirmPassword must match newPassword.", result.Error.Message);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_ReturnsUnauthorized_WhenCurrentUserIsInvalid()
    {
        var context = CreateContext();

        var result = await context.Service.ChangeCurrentPasswordAsync(ValidChangePasswordRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidCurrentUser", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_ReturnsForbidden_WhenAdminUserIsDisabled()
    {
        var adminUser = CreateAdminUser(isActive: false);
        var context = CreateContext(adminUser);
        context.CurrentUser.IsAuthenticated = true;
        context.CurrentUser.UserId = adminUser.Id.ToString();

        var result = await context.Service.ChangeCurrentPasswordAsync(ValidChangePasswordRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.Disabled", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_ReturnsUnauthorized_WhenCurrentPasswordIsInvalid()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);
        context.CurrentUser.IsAuthenticated = true;
        context.CurrentUser.UserId = adminUser.Id.ToString();

        var result = await context.Service.ChangeCurrentPasswordAsync(new ChangeCurrentAdminUserPasswordRequest
        {
            CurrentPassword = "wrong",
            NewPassword = "new-secret",
            ConfirmPassword = "new-secret"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("Identity.AdminAuth.InvalidCredentials", result.Error.Code);
        Assert.Equal("hashed:secret", adminUser.PasswordHash);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_ChangesPasswordAndSaves_WhenRequestIsValid()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);
        context.CurrentUser.IsAuthenticated = true;
        context.CurrentUser.UserId = adminUser.Id.ToString();

        var result = await context.Service.ChangeCurrentPasswordAsync(ValidChangePasswordRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed:new-secret", adminUser.PasswordHash);
        Assert.Equal(1, context.PasswordHashingService.HashCount);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    private static TestContext CreateContext(params AdminUser[] adminUsers)
    {
        return CreateContext(adminUsers, new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero));
    }

    private static TestContext CreateContext(AdminUser adminUser, DateTimeOffset utcNow)
    {
        return CreateContext([adminUser], utcNow);
    }

    private static TestContext CreateContext(AdminUser[] adminUsers, DateTimeOffset utcNow)
    {
        var repository = new InMemoryAdminUserRepository(adminUsers);
        var adminUserPermissionRepository = new TestAdminUserPermissionRepository();
        var unitOfWork = new TestUnitOfWork();
        var passwordHashingService = new TestPasswordHashingService();
        var accessTokenService = new TestAccessTokenService();
        var currentUser = new TestCurrentUser();
        var service = new AdminAuthApplicationService(
            repository,
            adminUserPermissionRepository,
            new AdminAccessTokenClaimsFactory(adminUserPermissionRepository),
            unitOfWork,
            passwordHashingService,
            accessTokenService,
            accessTokenService,
            accessTokenService,
            currentUser,
            new TestTimeProvider(utcNow),
            new ChangeCurrentAdminUserPasswordRequestValidator(),
            new RefreshAdminUserTokenRequestValidator(),
            new LoginAdminUserRequestValidator());

        return new TestContext(
            service,
            adminUserPermissionRepository,
            unitOfWork,
            passwordHashingService,
            accessTokenService,
            currentUser);
    }

    private static AdminUser CreateAdminUser(
        bool isActive = true,
        string passwordHash = "hashed:secret")
    {
        return AdminUser.Create(
            Guid.NewGuid(),
            "admin",
            "admin@example.com",
            "Admin",
            null,
            passwordHash,
            isActive);
    }

    private static ChangeCurrentAdminUserPasswordRequest ValidChangePasswordRequest()
    {
        return new ChangeCurrentAdminUserPasswordRequest
        {
            CurrentPassword = "secret",
            NewPassword = "new-secret",
            ConfirmPassword = "new-secret"
        };
    }

    private sealed record TestContext(
        AdminAuthApplicationService Service,
        TestAdminUserPermissionRepository AdminUserPermissionRepository,
        TestUnitOfWork UnitOfWork,
        TestPasswordHashingService PasswordHashingService,
        TestAccessTokenService AccessTokenService,
        TestCurrentUser CurrentUser);
}
