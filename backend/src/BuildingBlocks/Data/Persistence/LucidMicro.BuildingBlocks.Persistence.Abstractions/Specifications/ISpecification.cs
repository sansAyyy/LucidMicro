using System.Linq.Expressions;

namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;

public interface ISpecification<TEntity>
{
    Expression<Func<TEntity, bool>>? Criteria { get; }

    IReadOnlyList<Expression<Func<TEntity, object>>> Includes { get; }

    IReadOnlyList<OrderExpression<TEntity>> OrderExpressions { get; }

    int? Skip { get; }

    int? Take { get; }

    bool IsPagingEnabled { get; }

    bool AsNoTracking { get; }
}

