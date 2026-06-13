using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;
using LucidMicro.BuildingBlocks.Persistence.EFCore.DbContexts;
using LucidMicro.BuildingBlocks.Persistence.EFCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Interceptors;
using LucidMicro.Tests.Shared.Persistence;
using LucidMicro.Tests.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Tests.Persistence;

public sealed class EfCoreConventionsTests
{
    [Fact]
    public async Task ModelCreating_AppliesAuditAndSoftDeleteColumnConventions()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);

        var table = StoreObjectIdentifier.Table("test_entities", null);

        Assert.Equal("created_at", GetColumnName(entityType, nameof(TestEntity.CreatedAt), table));
        Assert.Equal("created_by", GetColumnName(entityType, nameof(TestEntity.CreatedBy), table));
        Assert.Equal("last_modified_at", GetColumnName(entityType, nameof(TestEntity.LastModifiedAt), table));
        Assert.Equal("last_modified_by", GetColumnName(entityType, nameof(TestEntity.LastModifiedBy), table));
        Assert.Equal(SoftDeleteRelationalConventions.IsDeletedColumnName, GetColumnName(entityType, nameof(TestEntity.IsDeleted), table));
    }

    [Fact]
    public async Task ModelCreating_AppliesSoftDeleteFilterToUniqueIndexes()
    {
        await using var scope = await CreateContextScopeAsync();
        var entityType = scope.Context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);

        var index = entityType
            .GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == nameof(TestEntity.Name)));

        Assert.True(index.IsUnique);
        Assert.Equal(SoftDeleteRelationalConventions.IsNotDeletedFilter, index.GetFilter());
    }

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedEntities()
    {
        await using var scope = await CreateContextScopeAsync();
        var activeEntity = new TestEntity(Guid.NewGuid(), "active");
        var deletedEntity = new TestEntity(Guid.NewGuid(), "deleted");
        deletedEntity.MarkDeleted();

        scope.Context.TestEntities.AddRange(activeEntity, deletedEntity);
        await scope.Context.SaveChangesAsync();

        var filteredEntities = await scope.Context.TestEntities.ToArrayAsync();
        var allEntities = await scope.Context.TestEntities.IgnoreQueryFilters().ToArrayAsync();

        Assert.Single(filteredEntities);
        Assert.Equal(activeEntity.Id, filteredEntities[0].Id);
        Assert.Equal(2, allEntities.Length);
    }

    [Fact]
    public async Task AuditInterceptor_MarksCreatedValues_WhenEntityIsAdded()
    {
        var now = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        await using var scope = await CreateContextScopeAsync("tester", now);
        var entity = new TestEntity(Guid.NewGuid(), "created");

        scope.Context.TestEntities.Add(entity);
        await scope.Context.SaveChangesAsync();

        Assert.Equal(now, entity.CreatedAt);
        Assert.Equal("tester", entity.CreatedBy);
        Assert.Null(entity.LastModifiedAt);
        Assert.Null(entity.LastModifiedBy);
    }

    [Fact]
    public async Task AuditInterceptor_MarksModifiedValues_WhenEntityIsUpdated()
    {
        var createdAt = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        var modifiedAt = new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(createdAt);
        await using var scope = await CreateContextScopeAsync("tester", timeProvider);
        var entity = new TestEntity(Guid.NewGuid(), "before");
        scope.Context.TestEntities.Add(entity);
        await scope.Context.SaveChangesAsync();

        timeProvider.UtcNow = modifiedAt;
        entity.Rename("after");
        await scope.Context.SaveChangesAsync();

        Assert.Equal(createdAt, entity.CreatedAt);
        Assert.Equal("tester", entity.CreatedBy);
        Assert.Equal(modifiedAt, entity.LastModifiedAt);
        Assert.Equal("tester", entity.LastModifiedBy);
    }

    [Fact]
    public async Task AuditInterceptor_ConvertsDeleteToSoftDelete()
    {
        var createdAt = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        var deletedAt = new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(createdAt);
        await using var scope = await CreateContextScopeAsync("tester", timeProvider);
        var entity = new TestEntity(Guid.NewGuid(), "deleted");
        scope.Context.TestEntities.Add(entity);
        await scope.Context.SaveChangesAsync();

        timeProvider.UtcNow = deletedAt;
        scope.Context.TestEntities.Remove(entity);
        await scope.Context.SaveChangesAsync();

        var filteredCount = await scope.Context.TestEntities.CountAsync();
        var storedEntity = await scope.Context.TestEntities
            .IgnoreQueryFilters()
            .SingleAsync();

        Assert.Equal(0, filteredCount);
        Assert.True(storedEntity.IsDeleted);
        Assert.Equal(deletedAt, storedEntity.LastModifiedAt);
        Assert.Equal("tester", storedEntity.LastModifiedBy);
    }

    [Fact]
    public void AddLucidEfCorePersistence_RegistersReadOnlyRepository()
    {
        var services = new ServiceCollection();
        services.AddLucidEfCorePersistence<TestDbContext>(
            options => options.UseSqlite("Data Source=:memory:"));

        using var serviceProvider = services.BuildServiceProvider();

        var repository = serviceProvider.GetRequiredService<IReadOnlyRepository<TestEntity, Guid>>();

        Assert.NotNull(repository);
    }

    private static string? GetColumnName(
        IEntityType entityType,
        string propertyName,
        StoreObjectIdentifier table)
    {
        return entityType.FindProperty(propertyName)?.GetColumnName(table);
    }

    private sealed class TestDbContext : LucidDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.ToTable("test_entities");
                entity.HasKey(testEntity => testEntity.Id);
                entity.Property(testEntity => testEntity.Name)
                    .HasColumnName("name")
                    .HasMaxLength(128)
                    .IsRequired();
                entity.HasIndex(testEntity => testEntity.Name)
                    .IsUnique()
                    .HasSoftDeleteFilter();
            });

            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class TestEntity : SoftDeleteEntity<Guid>
    {
        private TestEntity()
        {
        }

        public TestEntity(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Name { get; private set; } = string.Empty;

        public void Rename(string name)
        {
            Name = name;
        }
    }

    private static Task<SqliteDbContextScope<TestDbContext>> CreateContextScopeAsync()
    {
        return CreateContextScopeAsync(userId: null, TimeProvider.System);
    }

    private static Task<SqliteDbContextScope<TestDbContext>> CreateContextScopeAsync(
        string userId,
        DateTimeOffset utcNow)
    {
        return CreateContextScopeAsync(userId, new TestTimeProvider(utcNow));
    }

    private static Task<SqliteDbContextScope<TestDbContext>> CreateContextScopeAsync(
        string? userId,
        TimeProvider timeProvider)
    {
        return SqliteDbContextScope<TestDbContext>.CreateAsync(connection =>
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new AuditSaveChangesInterceptor(new TestAuditUserProvider(userId), timeProvider))
                .Options;

            return new TestDbContext(options);
        });
    }
}
