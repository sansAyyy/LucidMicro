using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Specifications;

public static class SpecificationEvaluator<TEntity>
    where TEntity : class
{
    public static IQueryable<TEntity> GetQuery(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity>? specification,
        bool evaluatePagination = true)
    {
        ArgumentNullException.ThrowIfNull(inputQuery);

        if (specification is null)
        {
            return inputQuery;
        }

        var query = inputQuery;

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        query = ApplyOrdering(query, specification.OrderExpressions);

        if (evaluatePagination && specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip!.Value).Take(specification.Take!.Value);
        }

        return query;
    }

    private static IQueryable<TEntity> ApplyOrdering(
        IQueryable<TEntity> query,
        IReadOnlyList<OrderExpression<TEntity>> orderExpressions)
    {
        if (orderExpressions.Count == 0)
        {
            return query;
        }

        IOrderedQueryable<TEntity>? orderedQuery = null;

        for (var index = 0; index < orderExpressions.Count; index++)
        {
            var orderExpression = orderExpressions[index];

            orderedQuery = index == 0
                ? ApplyPrimaryOrdering(query, orderExpression)
                : ApplyThenOrdering(orderedQuery!, orderExpression);
        }

        return orderedQuery!;
    }

    private static IOrderedQueryable<TEntity> ApplyPrimaryOrdering(
        IQueryable<TEntity> query,
        OrderExpression<TEntity> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.OrderBy(orderExpression.KeySelector)
            : query.OrderByDescending(orderExpression.KeySelector);
    }

    private static IOrderedQueryable<TEntity> ApplyThenOrdering(
        IOrderedQueryable<TEntity> query,
        OrderExpression<TEntity> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.ThenBy(orderExpression.KeySelector)
            : query.ThenByDescending(orderExpression.KeySelector);
    }
}
