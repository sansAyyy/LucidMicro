using System.Linq.Expressions;
using LucidMicro.BuildingBlocks.Domain.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;

internal static class SoftDeleteQueryFilterExtensions
{
    private const string FilterName = "SoftDelete";

    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
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
                .HasQueryFilter(FilterName, CreateFilter(entityType.ClrType));
        }
    }

    private static LambdaExpression CreateFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "entity");
        var isDeleted = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(bool)],
            parameter,
            Expression.Constant(nameof(ISoftDelete.IsDeleted)));

        return Expression.Lambda(Expression.Equal(isDeleted, Expression.Constant(false)), parameter);
    }
}
