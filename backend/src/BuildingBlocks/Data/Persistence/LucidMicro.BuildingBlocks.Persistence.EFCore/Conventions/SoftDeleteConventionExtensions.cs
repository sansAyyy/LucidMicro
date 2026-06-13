using LucidMicro.BuildingBlocks.Domain.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;

internal static class SoftDeleteConventionExtensions
{
    public static void ApplySoftDeleteConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is not null
                || entityType.IsOwned()
                || !typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .Property<bool>(nameof(ISoftDelete.IsDeleted))
                .HasColumnName(SoftDeleteRelationalConventions.IsDeletedColumnName)
                .IsRequired();
        }
    }
}
