using System.Text.Json;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Serialization;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Services;
using LucidMicro.Contracts.Notification;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Services;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Validators;
using LucidMicro.Services.Identity.Application.Tests.TestDoubles;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Tests;

public sealed class AdminUserApplicationServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenRequestIsInvalid()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(new CreateAdminUserRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.Validation", result.Error.Code);
        Assert.Empty(context.Repository.Items);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
        Assert.Equal(0, context.PasswordHashingService.HashCount);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenUserNameAlreadyExists()
    {
        var context = CreateContext(CreateAdminUser(userName: "admin", email: "other@example.com"));

        var result = await context.Service.CreateAsync(new CreateAdminUserRequest
        {
            UserName = " admin ",
            Email = "admin@example.com",
            DisplayName = "Admin",
            Password = "password"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.UserNameConflict", result.Error.Code);
        Assert.Single(context.Repository.Items);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenPhoneNumberAlreadyExists()
    {
        var context = CreateContext(CreateAdminUser(phoneNumber: "13800138000"));

        var result = await context.Service.CreateAsync(new CreateAdminUserRequest
        {
            UserName = "root",
            Email = "root@example.com",
            DisplayName = "Root",
            PhoneNumber = " 13800138000 ",
            Password = "password"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.PhoneNumberConflict", result.Error.Code);
        Assert.Single(context.Repository.Items);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateAsync_AddsAdminUser_WithNormalizedValuesAndHashedPassword()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(new CreateAdminUserRequest
        {
            UserName = " admin ",
            Email = " admin@example.com ",
            DisplayName = " Admin ",
            PhoneNumber = " ",
            Password = "secret",
            IsActive = true
        });

        Assert.True(result.IsSuccess);
        Assert.Single(context.Repository.Items);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
        Assert.Equal(1, context.PasswordHashingService.HashCount);

        var adminUser = context.Repository.Items.Single();
        Assert.Equal(result.Value.Id, adminUser.Id);
        Assert.Equal("admin", adminUser.UserName);
        Assert.Equal("admin@example.com", adminUser.Email);
        Assert.Equal("Admin", adminUser.DisplayName);
        Assert.Null(adminUser.PhoneNumber);
        Assert.Equal("hashed:secret", adminUser.PasswordHash);

        var outboxMessage = Assert.Single(context.OutboxMessages.Items);
        Assert.Equal("notification.send-requested.v1", outboxMessage.Type);

        using var payload = JsonDocument.Parse(outboxMessage.Payload);
        Assert.Equal("admin@example.com", payload.RootElement.GetProperty("recipient").GetString());
        Assert.Equal(NotificationChannels.InApp, payload.RootElement.GetProperty("channel").GetString());
        Assert.Equal("Admin account created", payload.RootElement.GetProperty("subject").GetString());
        Assert.Equal(
            "Your admin account 'admin' has been created.",
            payload.RootElement.GetProperty("content").GetString());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenAdminUserDoesNotExist()
    {
        var context = CreateContext();
        var id = Guid.NewGuid();

        var result = await context.Service.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedAdminUser_WhenAdminUserExists()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);
        var roleId = Guid.NewGuid();
        await context.AdminUserRoleRepository.ReplaceRolesAsync(adminUser.Id, [roleId]);

        var result = await context.Service.GetByIdAsync(adminUser.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(adminUser.Id, result.Value.Id);
        Assert.Equal(adminUser.UserName, result.Value.UserName);
        Assert.Equal(adminUser.Email, result.Value.Email);
        var role = Assert.Single(result.Value.Roles);
        Assert.Equal(roleId, role.Id);
    }

    [Fact]
    public async Task GetListAsync_AppliesKeywordAndPaging()
    {
        var firstMatch = CreateAdminUser(userName: "root", email: "root@example.com", displayName: "admin root");
        var secondMatch = CreateAdminUser(userName: "admin", email: "admin@example.com", displayName: "Administrator");
        var nonMatch = CreateAdminUser(userName: "operator", email: "operator@example.com", displayName: "Operator");
        firstMatch.MarkCreated(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        secondMatch.MarkCreated(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
        nonMatch.MarkCreated(new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero));
        var context = CreateContext(firstMatch, secondMatch, nonMatch);

        var result = await context.Service.GetListAsync(new GetAdminUsersRequest
        {
            Keyword = "admin",
            PageNumber = 1,
            PageSize = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(1, result.Value.PageSize);
        Assert.Single(result.Value.Items);
        Assert.Equal(secondMatch.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenRequestIsInvalid()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.UpdateAsync(adminUser.Id, new UpdateAdminUserRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenAdminUserDoesNotExist()
    {
        var context = CreateContext();

        var result = await context.Service.UpdateAsync(Guid.NewGuid(), ValidUpdateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.NotFound", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenEmailBelongsToAnotherAdminUser()
    {
        var adminUser = CreateAdminUser(userName: "admin", email: "admin@example.com");
        var anotherAdminUser = CreateAdminUser(userName: "root", email: "root@example.com");
        var context = CreateContext(adminUser, anotherAdminUser);

        var result = await context.Service.UpdateAsync(adminUser.Id, ValidUpdateRequest(email: "root@example.com"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.EmailConflict", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenPhoneNumberBelongsToAnotherAdminUser()
    {
        var adminUser = CreateAdminUser(userName: "admin", email: "admin@example.com", phoneNumber: "13800138000");
        var anotherAdminUser = CreateAdminUser(userName: "root", email: "root@example.com", phoneNumber: "13900139000");
        var context = CreateContext(adminUser, anotherAdminUser);

        var result = await context.Service.UpdateAsync(
            adminUser.Id,
            ValidUpdateRequest(phoneNumber: " 13900139000 "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.PhoneNumberConflict", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAdminUser_WithNormalizedValues()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.UpdateAsync(adminUser.Id, new UpdateAdminUserRequest
        {
            UserName = " new-admin ",
            Email = " new@example.com ",
            DisplayName = " New Admin ",
            PhoneNumber = " 123 ",
            IsActive = false
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
        Assert.Equal("new-admin", adminUser.UserName);
        Assert.Equal("new@example.com", adminUser.Email);
        Assert.Equal("New Admin", adminUser.DisplayName);
        Assert.Equal("123", adminUser.PhoneNumber);
        Assert.False(adminUser.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAdminUserAndSaves_WhenAdminUserExists()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.DeleteAsync(adminUser.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(context.Repository.Items);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ActivateAsync_ActivatesAdminUserAndSaves()
    {
        var adminUser = CreateAdminUser(isActive: false);
        var context = CreateContext(adminUser);

        var result = await context.Service.ActivateAsync(adminUser.Id);

        Assert.True(result.IsSuccess);
        Assert.True(adminUser.IsActive);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task DeactivateAsync_DeactivatesAdminUserAndSaves()
    {
        var adminUser = CreateAdminUser(isActive: true);
        var context = CreateContext(adminUser);

        var result = await context.Service.DeactivateAsync(adminUser.Id);

        Assert.True(result.IsSuccess);
        Assert.False(adminUser.IsActive);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsValidationError_WhenRequestIsInvalid()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.ResetPasswordAsync(adminUser.Id, new ResetAdminUserPasswordRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.Validation", result.Error.Code);
        Assert.Equal("password-hash", adminUser.PasswordHash);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
        Assert.Equal(0, context.PasswordHashingService.HashCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsValidationError_WhenConfirmPasswordDoesNotMatch()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.ResetPasswordAsync(adminUser.Id, new ResetAdminUserPasswordRequest
        {
            NewPassword = "new-secret",
            ConfirmPassword = "different"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("confirmPassword must match newPassword.", result.Error.Message);
        Assert.Equal("password-hash", adminUser.PasswordHash);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
        Assert.Equal(0, context.PasswordHashingService.HashCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsNotFound_WhenAdminUserDoesNotExist()
    {
        var context = CreateContext();
        var id = Guid.NewGuid();

        var result = await context.Service.ResetPasswordAsync(id, ValidResetPasswordRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.NotFound", result.Error.Code);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
        Assert.Equal(0, context.PasswordHashingService.HashCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_ChangesPasswordAndSaves_WhenAdminUserExists()
    {
        var adminUser = CreateAdminUser();
        var context = CreateContext(adminUser);

        var result = await context.Service.ResetPasswordAsync(adminUser.Id, ValidResetPasswordRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed:new-secret", adminUser.PasswordHash);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
        Assert.Equal(1, context.PasswordHashingService.HashCount);
    }

    [Fact]
    public async Task AssignRolesAsync_ReturnsNotFound_WhenAdminUserDoesNotExist()
    {
        var context = CreateContext();

        var result = await context.Service.AssignRolesAsync(Guid.NewGuid(), new AssignAdminUserRolesRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.NotFound", result.Error.Code);
        Assert.Equal(0, context.AdminUserRoleRepository.ReplaceCount);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task AssignRolesAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        var adminUser = CreateAdminUser();
        var missingRoleId = Guid.NewGuid();
        var context = CreateContext([adminUser], []);

        var result = await context.Service.AssignRolesAsync(adminUser.Id, new AssignAdminUserRolesRequest
        {
            RoleIds = [missingRoleId]
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Identity.AdminUsers.RoleNotFound", result.Error.Code);
        Assert.Equal(0, context.AdminUserRoleRepository.ReplaceCount);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task AssignRolesAsync_ReplacesDistinctRoleIdsAndSaves()
    {
        var adminUser = CreateAdminUser();
        var role = CreateRole();
        var context = CreateContext([adminUser], [role]);

        var result = await context.Service.AssignRolesAsync(adminUser.Id, new AssignAdminUserRolesRequest
        {
            RoleIds = [role.Id, role.Id]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, context.AdminUserRoleRepository.ReplaceCount);
        Assert.Equal([role.Id], context.AdminUserRoleRepository.RoleIdsByAdminUserId[adminUser.Id]);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    private static TestContext CreateContext(params AdminUser[] adminUsers)
    {
        return CreateContext(adminUsers, []);
    }

    private static TestContext CreateContext(IReadOnlyList<AdminUser> adminUsers, IReadOnlyList<Role> roles)
    {
        var repository = new InMemoryAdminUserRepository(adminUsers);
        var roleRepository = new InMemoryRoleRepository(roles);
        var adminUserRoleRepository = new TestAdminUserRoleRepository();
        var unitOfWork = new TestUnitOfWork();
        var outboxMessages = new TestOutboxMessageStore();
        var passwordHashingService = new TestPasswordHashingService();
        var service = new AdminUserApplicationService(
            repository,
            roleRepository,
            adminUserRoleRepository,
            unitOfWork,
            new DefaultOutboxEventWriter(outboxMessages, new SystemTextJsonOutboxMessageSerializer()),
            passwordHashingService,
            new CreateAdminUserRequestValidator(),
            new ResetAdminUserPasswordRequestValidator(),
            new UpdateAdminUserRequestValidator());

        return new TestContext(service, repository, adminUserRoleRepository, unitOfWork, outboxMessages, passwordHashingService);
    }

    private static AdminUser CreateAdminUser(
        string userName = "admin",
        string email = "admin@example.com",
        string displayName = "Admin",
        string? phoneNumber = null,
        bool isActive = true)
    {
        return AdminUser.Create(
            Guid.NewGuid(),
            userName,
            email,
            displayName,
            phoneNumber,
            "password-hash",
            isActive);
    }

    private static UpdateAdminUserRequest ValidUpdateRequest(
        string email = "admin@example.com",
        string? phoneNumber = null)
    {
        return new UpdateAdminUserRequest
        {
            UserName = "admin",
            Email = email,
            DisplayName = "Admin",
            PhoneNumber = phoneNumber,
            IsActive = true
        };
    }

    private static ResetAdminUserPasswordRequest ValidResetPasswordRequest()
    {
        return new ResetAdminUserPasswordRequest
        {
            NewPassword = "new-secret",
            ConfirmPassword = "new-secret"
        };
    }

    private static Role CreateRole()
    {
        return Role.Create(
            Guid.NewGuid(),
            "operator",
            "Operator",
            null,
            isSystem: false,
            isEnabled: true);
    }

    private sealed record TestContext(
        AdminUserApplicationService Service,
        InMemoryAdminUserRepository Repository,
        TestAdminUserRoleRepository AdminUserRoleRepository,
        TestUnitOfWork UnitOfWork,
        TestOutboxMessageStore OutboxMessages,
        TestPasswordHashingService PasswordHashingService);
}
