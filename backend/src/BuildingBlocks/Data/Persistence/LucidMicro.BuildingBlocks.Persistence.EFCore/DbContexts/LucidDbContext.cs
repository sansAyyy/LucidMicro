using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.DbContexts;

public abstract class LucidDbContext : DbContext, IUnitOfWork
{
    protected LucidDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyAuditConventions();
        modelBuilder.ApplySoftDeleteConventions();
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
