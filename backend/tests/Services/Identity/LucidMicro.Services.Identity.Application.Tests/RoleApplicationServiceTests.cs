using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.Roles.Services;
using LucidMicro.Services.Identity.Application.Features.Roles.Validators;
using LucidMicro.Services.Identity.Application.Tests.TestDoubles;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Tests;

public sealed class RoleApplicationServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenRequestIsInvalid()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(new CreateRoleRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Identity.Roles.Validation", result.Error.Code);
        Assert.Empty(context.Roles.Items);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenCodeAlreadyExists()
    {
        var context = CreateContext(CreateRole(code: "operator"));

        var result = await context.Service.CreateAsync(new CreateRoleRequest
        {
            Code = " operator ",
            Name = "Operator"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Identity.Roles.CodeConflict", result.Error.Code);
        Assert.Single(context.Roles.Items);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateAsync_AddsRole_WithNormalizedValues()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(new CreateRoleRequest
        {
            Code = " operator ",
            Name = " Operator ",
            Description = " ",
            IsEnabled = true
        });

        Assert.True(result.IsSuccess);
        Assert.Single(context.Roles.Items);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);

        var role = context.Roles.Items.Single();
        Assert.Equal(result.Value.Id, role.Id);
        Assert.Equal("operator", role.Code);
        Assert.Equal("Operator", role.Name);
        Assert.Null(role.Description);
        Assert.False(role.IsSystem);
        Assert.True(role.IsEnabled);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPermissionIds_WhenRoleExists()
    {
        var role = CreateRole();
        var permissionId = Guid.NewGuid();
        var context = CreateContext(role);
        context.RolePermissionRepository.SetPermissions(role.Id, permissionId);

        var result = await context.Service.GetByIdAsync(role.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(role.Id, result.Value.Id);
        Assert.Equal([permissionId], result.Value.PermissionIds);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesRole_WithNormalizedValues()
    {
        var role = CreateRole();
        var context = CreateContext(role);

        var result = await context.Service.UpdateAsync(role.Id, new UpdateRoleRequest
        {
            Name = " New Role ",
            Description = " New description ",
            IsEnabled = false
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("New Role", role.Name);
        Assert.Equal("New description", role.Description);
        Assert.False(role.IsEnabled);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRoleAndSaves()
    {
        var role = CreateRole();
        var context = CreateContext(role);

        var result = await context.Service.DeleteAsync(role.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(context.Roles.Items);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task AssignPermissionsAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        var context = CreateContext();

        var result = await context.Service.AssignPermissionsAsync(Guid.NewGuid(), new AssignRolePermissionsRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Identity.Roles.NotFound", result.Error.Code);
        Assert.Equal(0, context.RolePermissionRepository.ReplaceCount);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task AssignPermissionsAsync_ReturnsNotFound_WhenPermissionDoesNotExist()
    {
        var role = CreateRole();
        var missingPermissionId = Guid.NewGuid();
        var context = CreateContext([role], []);

        var result = await context.Service.AssignPermissionsAsync(role.Id, new AssignRolePermissionsRequest
        {
            PermissionIds = [missingPermissionId]
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Identity.Roles.PermissionNotFound", result.Error.Code);
        Assert.Equal(0, context.RolePermissionRepository.ReplaceCount);
        Assert.Equal(0, context.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task AssignPermissionsAsync_ReplacesDistinctPermissionIdsAndSaves()
    {
        var role = CreateRole();
        var permission = CreatePermission();
        var context = CreateContext([role], [permission]);

        var result = await context.Service.AssignPermissionsAsync(role.Id, new AssignRolePermissionsRequest
        {
            PermissionIds = [permission.Id, permission.Id]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, context.RolePermissionRepository.ReplaceCount);
        Assert.Equal([permission.Id], context.RolePermissionRepository.PermissionIdsByRoleId[role.Id]);
        Assert.Equal(1, context.UnitOfWork.SaveChangesCount);
    }

    private static TestContext CreateContext(params Role[] roles)
    {
        return CreateContext(roles, []);
    }

    private static TestContext CreateContext(IReadOnlyList<Role> roles, IReadOnlyList<Permission> permissions)
    {
        var roleRepository = new InMemoryRoleRepository(roles);
        var permissionRepository = new InMemoryPermissionRepository(permissions);
        var rolePermissionRepository = new TestRolePermissionRepository();
        var unitOfWork = new TestUnitOfWork();
        var service = new RoleApplicationService(
            roleRepository,
            permissionRepository,
            rolePermissionRepository,
            unitOfWork,
            new CreateRoleRequestValidator(),
            new UpdateRoleRequestValidator());

        return new TestContext(service, roleRepository, rolePermissionRepository, unitOfWork);
    }

    private static Role CreateRole(string code = "operator")
    {
        return Role.Create(
            Guid.NewGuid(),
            code,
            "Operator",
            null,
            isSystem: false,
            isEnabled: true);
    }

    private static Permission CreatePermission()
    {
        return Permission.Create(
            Guid.NewGuid(),
            "identity.roles.read",
            "Read roles",
            null,
            "identity",
            "Identity",
            "roles",
            "Roles",
            "read",
            sortOrder: 10,
            isEnabled: true);
    }

    private sealed record TestContext(
        RoleApplicationService Service,
        InMemoryRoleRepository Roles,
        TestRolePermissionRepository RolePermissionRepository,
        TestUnitOfWork UnitOfWork);
}
