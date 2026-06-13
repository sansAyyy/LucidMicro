using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Interceptors;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Repositories;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Entities;
using LucidMicro.BuildingBlocks.Outbox.EFCore.ModelBuilding;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;
using LucidMicro.Services.Identity.Domain.Entities.Roles;
using LucidMicro.Services.Identity.Infrastructure.Persistence;
using LucidMicro.Services.Identity.Infrastructure.Persistence.Repositories;
using LucidMicro.Tests.Shared.Persistence;
using LucidMicro.Tests.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LucidMicro.Services.Identity.Infrastructure.Tests;

public sealed class IdentityDbContextTests
{
    [Fact]
    public async Task ModelCreating_ConfiguresAdminUserTableColumnsAndConstraints()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(AdminUser));
        Assert.NotNull(entityType);

        Assert.Equal("admin_users", entityType.GetTableName());

        var table = StoreObjectIdentifier.Table("admin_users", null);
        Assert.Equal("id", GetColumnName(entityType, nameof(AdminUser.Id), table));
        Assert.Equal("user_name", GetColumnName(entityType, nameof(AdminUser.UserName), table));
        Assert.Equal("email", GetColumnName(entityType, nameof(AdminUser.Email), table));
        Assert.Equal("display_name", GetColumnName(entityType, nameof(AdminUser.DisplayName), table));
        Assert.Equal("phone_number", GetColumnName(entityType, nameof(AdminUser.PhoneNumber), table));
        Assert.Equal("password_hash", GetColumnName(entityType, nameof(AdminUser.PasswordHash), table));
        Assert.Equal("is_active", GetColumnName(entityType, nameof(AdminUser.IsActive), table));
        Assert.Equal("last_login_at", GetColumnName(entityType, nameof(AdminUser.LastLoginAt), table));

        AssertRequiredTextProperty(entityType, nameof(AdminUser.UserName), maxLength: 64);
        AssertRequiredTextProperty(entityType, nameof(AdminUser.Email), maxLength: 256);
        AssertRequiredTextProperty(entityType, nameof(AdminUser.DisplayName), maxLength: 128);
        AssertRequiredTextProperty(entityType, nameof(AdminUser.PasswordHash), maxLength: 2048);

        var phoneNumberProperty = entityType.FindProperty(nameof(AdminUser.PhoneNumber));
        Assert.NotNull(phoneNumberProperty);
        Assert.True(phoneNumberProperty.IsNullable);
        Assert.Equal(32, phoneNumberProperty.GetMaxLength());
    }

    [Fact]
    public async Task ModelCreating_AppliesAuditAndSoftDeleteConventionsToAdminUser()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(AdminUser));
        Assert.NotNull(entityType);

        var table = StoreObjectIdentifier.Table("admin_users", null);

        Assert.Equal("created_at", GetColumnName(entityType, nameof(AdminUser.CreatedAt), table));
        Assert.Equal("created_by", GetColumnName(entityType, nameof(AdminUser.CreatedBy), table));
        Assert.Equal("last_modified_at", GetColumnName(entityType, nameof(AdminUser.LastModifiedAt), table));
        Assert.Equal("last_modified_by", GetColumnName(entityType, nameof(AdminUser.LastModifiedBy), table));
        Assert.Equal(SoftDeleteRelationalConventions.IsDeletedColumnName, GetColumnName(entityType, nameof(AdminUser.IsDeleted), table));
    }

    [Fact]
    public async Task ModelCreating_ConfiguresSoftDeleteFilteredUniqueIndexes()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(AdminUser));
        Assert.NotNull(entityType);

        AssertUniqueSoftDeleteIndex(entityType, nameof(AdminUser.UserName), "ix_admin_users_user_name");
        AssertUniqueSoftDeleteIndex(entityType, nameof(AdminUser.Email), "ix_admin_users_email");
        AssertUniqueSoftDeleteIndex(entityType, nameof(AdminUser.PhoneNumber), "ix_admin_users_phone_number");
    }

    [Fact]
    public async Task ModelCreating_ConfiguresPermissionTableColumnsAndConstraints()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(Permission));
        Assert.NotNull(entityType);

        Assert.Equal("permissions", entityType.GetTableName());

        var table = StoreObjectIdentifier.Table("permissions", null);
        Assert.Equal("id", GetColumnName(entityType, nameof(Permission.Id), table));
        Assert.Equal("code", GetColumnName(entityType, nameof(Permission.Code), table));
        Assert.Equal("name", GetColumnName(entityType, nameof(Permission.Name), table));
        Assert.Equal("description", GetColumnName(entityType, nameof(Permission.Description), table));
        Assert.Equal("group_code", GetColumnName(entityType, nameof(Permission.GroupCode), table));
        Assert.Equal("group_name", GetColumnName(entityType, nameof(Permission.GroupName), table));
        Assert.Equal("resource_code", GetColumnName(entityType, nameof(Permission.ResourceCode), table));
        Assert.Equal("resource_name", GetColumnName(entityType, nameof(Permission.ResourceName), table));
        Assert.Equal("action", GetColumnName(entityType, nameof(Permission.Action), table));
        Assert.Equal("sort_order", GetColumnName(entityType, nameof(Permission.SortOrder), table));
        Assert.Equal("is_enabled", GetColumnName(entityType, nameof(Permission.IsEnabled), table));

        AssertRequiredTextProperty(entityType, nameof(Permission.Code), maxLength: 128);
        AssertRequiredTextProperty(entityType, nameof(Permission.Name), maxLength: 128);
        AssertRequiredTextProperty(entityType, nameof(Permission.GroupCode), maxLength: 64);
        AssertRequiredTextProperty(entityType, nameof(Permission.GroupName), maxLength: 128);
        AssertRequiredTextProperty(entityType, nameof(Permission.ResourceCode), maxLength: 64);
        AssertRequiredTextProperty(entityType, nameof(Permission.ResourceName), maxLength: 128);
        AssertRequiredTextProperty(entityType, nameof(Permission.Action), maxLength: 64);

        AssertUniqueIndex(entityType, nameof(Permission.Code), "ix_permissions_code");
        AssertUniqueIndex(entityType, nameof(Permission.Action), "ix_permissions_group_resource_action");
    }

    [Fact]
    public async Task ModelCreating_ConfiguresRoleTableColumnsAndConstraints()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(Role));
        Assert.NotNull(entityType);

        Assert.Equal("roles", entityType.GetTableName());

        var table = StoreObjectIdentifier.Table("roles", null);
        Assert.Equal("id", GetColumnName(entityType, nameof(Role.Id), table));
        Assert.Equal("code", GetColumnName(entityType, nameof(Role.Code), table));
        Assert.Equal("name", GetColumnName(entityType, nameof(Role.Name), table));
        Assert.Equal("description", GetColumnName(entityType, nameof(Role.Description), table));
        Assert.Equal("is_system", GetColumnName(entityType, nameof(Role.IsSystem), table));
        Assert.Equal("is_enabled", GetColumnName(entityType, nameof(Role.IsEnabled), table));

        AssertRequiredTextProperty(entityType, nameof(Role.Code), maxLength: 64);
        AssertRequiredTextProperty(entityType, nameof(Role.Name), maxLength: 128);
        AssertUniqueSoftDeleteIndex(entityType, nameof(Role.Code), "ix_roles_code");
    }

    [Fact]
    public async Task ModelCreating_ConfiguresRolePermissionJoinTable()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(RolePermission));
        Assert.NotNull(entityType);

        Assert.Equal("role_permissions", entityType.GetTableName());
        AssertCompositeKey(entityType, nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId));

        var table = StoreObjectIdentifier.Table("role_permissions", null);
        Assert.Equal("role_id", GetColumnName(entityType, nameof(RolePermission.RoleId), table));
        Assert.Equal("permission_id", GetColumnName(entityType, nameof(RolePermission.PermissionId), table));

        Assert.NotNull(entityType.FindForeignKeys(entityType.FindProperty(nameof(RolePermission.RoleId))!).SingleOrDefault());
        Assert.NotNull(entityType.FindForeignKeys(entityType.FindProperty(nameof(RolePermission.PermissionId))!).SingleOrDefault());
    }

    [Fact]
    public async Task ModelCreating_ConfiguresAdminUserRoleJoinTable()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(AdminUserRole));
        Assert.NotNull(entityType);

        Assert.Equal("admin_user_roles", entityType.GetTableName());
        AssertCompositeKey(entityType, nameof(AdminUserRole.AdminUserId), nameof(AdminUserRole.RoleId));

        var table = StoreObjectIdentifier.Table("admin_user_roles", null);
        Assert.Equal("admin_user_id", GetColumnName(entityType, nameof(AdminUserRole.AdminUserId), table));
        Assert.Equal("role_id", GetColumnName(entityType, nameof(AdminUserRole.RoleId), table));

        Assert.NotNull(entityType.FindForeignKeys(entityType.FindProperty(nameof(AdminUserRole.AdminUserId))!).SingleOrDefault());
        Assert.NotNull(entityType.FindForeignKeys(entityType.FindProperty(nameof(AdminUserRole.RoleId))!).SingleOrDefault());
    }

    [Fact]
    public async Task ModelCreating_ConfiguresOutboxTable()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(OutboxMessageEntity));
        Assert.NotNull(entityType);

        var table = StoreObjectIdentifier.Table(OutboxModelBuilderExtensions.TableName, null);

        Assert.Equal(OutboxModelBuilderExtensions.TableName, entityType.GetTableName());
        Assert.Equal("id", GetColumnName(entityType, nameof(OutboxMessageEntity.Id), table));
        Assert.Equal("type", GetColumnName(entityType, nameof(OutboxMessageEntity.Type), table));
        Assert.Equal("payload", GetColumnName(entityType, nameof(OutboxMessageEntity.Payload), table));
        Assert.Equal("published_at", GetColumnName(entityType, nameof(OutboxMessageEntity.PublishedAt), table));
        Assert.Equal("locked_until", GetColumnName(entityType, nameof(OutboxMessageEntity.LockedUntil), table));
    }

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedAdminUsers()
    {
        await using var scope = await CreateContextScopeAsync();
        var activeAdminUser = CreateAdminUser("active", "active@example.com");
        var deletedAdminUser = CreateAdminUser("deleted", "deleted@example.com");
        deletedAdminUser.MarkDeleted();

        scope.Context.AdminUsers.AddRange(activeAdminUser, deletedAdminUser);
        await scope.Context.SaveChangesAsync();

        var filteredAdminUsers = await scope.Context.AdminUsers.ToArrayAsync();
        var allAdminUsers = await scope.Context.AdminUsers.IgnoreQueryFilters().ToArrayAsync();

        Assert.Single(filteredAdminUsers);
        Assert.Equal(activeAdminUser.Id, filteredAdminUsers[0].Id);
        Assert.Equal(2, allAdminUsers.Length);
    }

    [Fact]
    public async Task Repository_AddAndGetByIdAsync_PersistsAdminUserWithAuditValues()
    {
        var now = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        await using var scope = await CreateContextScopeAsync("tester", now);
        var repository = new EfRepository<AdminUser, Guid>(scope.Context);
        var adminUser = CreateAdminUser("admin", "admin@example.com");

        await repository.AddAsync(adminUser);
        await scope.Context.SaveChangesAsync();

        var storedAdminUser = await repository.GetByIdAsync(adminUser.Id);

        Assert.NotNull(storedAdminUser);
        Assert.Equal(adminUser.Id, storedAdminUser.Id);
        Assert.Equal("admin", storedAdminUser.UserName);
        Assert.Equal(now, storedAdminUser.CreatedAt);
        Assert.Equal("tester", storedAdminUser.CreatedBy);
    }

    [Fact]
    public async Task Repository_AnyAsync_UsesSpecification()
    {
        await using var scope = await CreateContextScopeAsync();
        var repository = new EfRepository<AdminUser, Guid>(scope.Context);
        await repository.AddRangeAsync(
        [
            CreateAdminUser("admin", "admin@example.com"),
            CreateAdminUser("root", "root@example.com")
        ]);
        await scope.Context.SaveChangesAsync();

        var exists = await repository.AnyAsync(new AdminUserByUserNameSpecification("admin"));
        var missing = await repository.AnyAsync(new AdminUserByUserNameSpecification("missing"));

        Assert.True(exists);
        Assert.False(missing);
    }

    [Fact]
    public async Task Repository_PageAsync_UsesSpecificationAndPageRequest()
    {
        await using var scope = await CreateContextScopeAsync();
        var repository = new EfRepository<AdminUser, Guid>(scope.Context);
        await repository.AddRangeAsync(
        [
            CreateAdminUser("root", "root@example.com"),
            CreateAdminUser("admin-1", "admin-1@example.com"),
            CreateAdminUser("admin-2", "admin-2@example.com")
        ]);
        await scope.Context.SaveChangesAsync();

        var page = await repository.PageAsync(
            new AdminUsersByKeywordSpecification("admin"),
            new PageRequest
            {
                PageNumber = 2,
                PageSize = 1
            });

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(1, page.PageSize);
        Assert.Single(page.Items);
        Assert.Equal("admin-2", page.Items[0].UserName);
    }

    [Fact]
    public async Task RolePermissionRepository_ReplacesRolePermissions()
    {
        await using var scope = await CreateContextScopeAsync();
        var role = CreateRole();
        var firstPermission = CreatePermission("identity.roles.read");
        var secondPermission = CreatePermission("identity.roles.manage");
        scope.Context.Roles.Add(role);
        scope.Context.Permissions.AddRange(firstPermission, secondPermission);
        await scope.Context.SaveChangesAsync();

        var repository = new RolePermissionRepository(scope.Context);
        await repository.ReplacePermissionsAsync(role.Id, [firstPermission.Id]);
        await scope.Context.SaveChangesAsync();
        await repository.ReplacePermissionsAsync(role.Id, [secondPermission.Id, secondPermission.Id]);
        await scope.Context.SaveChangesAsync();

        var permissionIds = await repository.GetPermissionIdsAsync(role.Id);

        Assert.Equal([secondPermission.Id], permissionIds);
        Assert.Single(scope.Context.RolePermissions);
    }

    [Fact]
    public async Task AdminUserRoleRepository_ReplacesAdminUserRoles()
    {
        await using var scope = await CreateContextScopeAsync();
        var adminUser = CreateAdminUser("admin", "admin@example.com");
        var firstRole = CreateRole("operator");
        var secondRole = CreateRole("viewer");
        scope.Context.AdminUsers.Add(adminUser);
        scope.Context.Roles.AddRange(firstRole, secondRole);
        await scope.Context.SaveChangesAsync();

        var repository = new AdminUserRoleRepository(scope.Context);
        await repository.ReplaceRolesAsync(adminUser.Id, [firstRole.Id]);
        await scope.Context.SaveChangesAsync();
        await repository.ReplaceRolesAsync(adminUser.Id, [secondRole.Id, secondRole.Id]);
        await scope.Context.SaveChangesAsync();

        var roles = await repository.GetRolesAsync(adminUser.Id);

        var role = Assert.Single(roles);
        Assert.Equal(secondRole.Id, role.Id);
        Assert.Equal(secondRole.Code, role.Code);
        Assert.Equal(secondRole.Name, role.Name);
        Assert.Equal(secondRole.IsEnabled, role.IsEnabled);
        Assert.Single(scope.Context.AdminUserRoles);
    }

    [Fact]
    public async Task AdminUserPermissionRepository_ReturnsDistinctEnabledPermissionCodesFromEnabledRoles()
    {
        await using var scope = await CreateContextScopeAsync();
        var adminUser = CreateAdminUser("admin", "admin@example.com");
        var enabledRole = CreateRole("operator");
        var disabledRole = CreateRole("disabled");
        disabledRole.Disable();
        var enabledPermission = CreatePermission("identity.roles.read", "roles", "read");
        var duplicatedPermission = CreatePermission("identity.admin-users.read", "admin-users", "read");
        var disabledPermission = CreatePermission("identity.roles.manage", "roles", "manage");
        disabledPermission.Disable();
        scope.Context.AdminUsers.Add(adminUser);
        scope.Context.Roles.AddRange(enabledRole, disabledRole);
        scope.Context.Permissions.AddRange(enabledPermission, duplicatedPermission, disabledPermission);
        scope.Context.AdminUserRoles.AddRange(
            AdminUserRole.Create(adminUser.Id, enabledRole.Id),
            AdminUserRole.Create(adminUser.Id, disabledRole.Id));
        scope.Context.RolePermissions.AddRange(
            RolePermission.Create(enabledRole.Id, enabledPermission.Id),
            RolePermission.Create(enabledRole.Id, duplicatedPermission.Id),
            RolePermission.Create(enabledRole.Id, disabledPermission.Id),
            RolePermission.Create(disabledRole.Id, duplicatedPermission.Id));
        await scope.Context.SaveChangesAsync();

        var repository = new ReadOnlyAdminUserPermissionRepository(scope.Context);
        var permissionCodes = await repository.GetPermissionCodesAsync(adminUser.Id);

        Assert.Equal(["identity.admin-users.read", "identity.roles.read"], permissionCodes);
    }

    private static AdminUser CreateAdminUser(string userName, string email)
    {
        return AdminUser.Create(
            Guid.NewGuid(),
            userName,
            email,
            userName,
            null,
            "password-hash",
            isActive: true);
    }

    private static Role CreateRole(string code = "operator")
    {
        return Role.Create(
            Guid.NewGuid(),
            code,
            code,
            null,
            isSystem: false,
            isEnabled: true);
    }

    private static Permission CreatePermission(string code, string resourceCode = "roles", string? action = null)
    {
        return Permission.Create(
            Guid.NewGuid(),
            code,
            code,
            null,
            "identity",
            "Identity",
            resourceCode,
            resourceCode,
            action ?? code[(code.LastIndexOf('.') + 1)..],
            sortOrder: 10,
            isEnabled: true);
    }

    private static string? GetColumnName(
        IEntityType entityType,
        string propertyName,
        StoreObjectIdentifier table)
    {
        return entityType.FindProperty(propertyName)?.GetColumnName(table);
    }

    private static void AssertRequiredTextProperty(IEntityType entityType, string propertyName, int maxLength)
    {
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.False(property.IsNullable);
        Assert.Equal(maxLength, property.GetMaxLength());
    }

    private static void AssertUniqueSoftDeleteIndex(
        IEntityType entityType,
        string propertyName,
        string databaseName)
    {
        var index = entityType
            .GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == propertyName));

        Assert.True(index.IsUnique);
        Assert.Equal(databaseName, index.GetDatabaseName());
        Assert.Equal(SoftDeleteRelationalConventions.IsNotDeletedFilter, index.GetFilter());
    }

    private static void AssertUniqueIndex(
        IEntityType entityType,
        string propertyName,
        string databaseName)
    {
        var index = entityType
            .GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == propertyName));

        Assert.True(index.IsUnique);
        Assert.Equal(databaseName, index.GetDatabaseName());
    }

    private static void AssertCompositeKey(IEntityType entityType, params string[] propertyNames)
    {
        var key = entityType.FindPrimaryKey();
        Assert.NotNull(key);
        Assert.Equal(propertyNames, key.Properties.Select(property => property.Name));
    }

    private sealed class AdminUserByUserNameSpecification : Specification<AdminUser>
    {
        public AdminUserByUserNameSpecification(string userName)
        {
            Where(adminUser => adminUser.UserName == userName);
        }
    }

    private sealed class AdminUsersByKeywordSpecification : Specification<AdminUser>
    {
        public AdminUsersByKeywordSpecification(string keyword)
        {
            Where(adminUser => adminUser.UserName.Contains(keyword));
            OrderBy(adminUser => adminUser.UserName);
        }
    }

    private static Task<SqliteDbContextScope<IdentityDbContext>> CreateContextScopeAsync()
    {
        return CreateContextScopeAsync(userId: null, TimeProvider.System);
    }

    private static Task<SqliteDbContextScope<IdentityDbContext>> CreateContextScopeAsync(
        string userId,
        DateTimeOffset utcNow)
    {
        return CreateContextScopeAsync(userId, new TestTimeProvider(utcNow));
    }

    private static Task<SqliteDbContextScope<IdentityDbContext>> CreateContextScopeAsync(
        string? userId,
        TimeProvider timeProvider)
    {
        return SqliteDbContextScope<IdentityDbContext>.CreateAsync(connection =>
        {
            var options = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new AuditSaveChangesInterceptor(new TestAuditUserProvider(userId), timeProvider))
                .Options;

            return new IdentityDbContext(options);
        });
    }
}
