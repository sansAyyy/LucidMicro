using System.Linq.Expressions;

namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;

public sealed record OrderExpression<TEntity>(
    Expression<Func<TEntity, object>> KeySelector,
    OrderDirection Direction);

