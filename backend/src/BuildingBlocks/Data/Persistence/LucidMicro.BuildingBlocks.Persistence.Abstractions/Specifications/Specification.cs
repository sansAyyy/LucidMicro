using System.Linq.Expressions;

namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;

public abstract class Specification<TEntity> : ISpecification<TEntity>
{
    private readonly List<Expression<Func<TEntity, object>>> _includes = [];
    private readonly List<OrderExpression<TEntity>> _orderExpressions = [];

    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }

    public IReadOnlyList<Expression<Func<TEntity, object>>> Includes => _includes;

    public IReadOnlyList<OrderExpression<TEntity>> OrderExpressions => _orderExpressions;

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool IsPagingEnabled { get; private set; }

    public bool AsNoTracking { get; private set; }

    protected void Where(Expression<Func<TEntity, bool>> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        Criteria = criteria;
    }

    protected void Include(Expression<Func<TEntity, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);

        _includes.Add(includeExpression);
    }

    protected void OrderBy(Expression<Func<TEntity, object>> keySelector)
    {
        AddOrderExpression(keySelector, OrderDirection.Ascending);
    }

    protected void OrderByDescending(Expression<Func<TEntity, object>> keySelector)
    {
        AddOrderExpression(keySelector, OrderDirection.Descending);
    }

    protected void ApplyPaging(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip cannot be negative.");
        }

        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");
        }

        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void ApplyNoTracking()
    {
        AsNoTracking = true;
    }

    private void AddOrderExpression(Expression<Func<TEntity, object>> keySelector, OrderDirection direction)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        _orderExpressions.Add(new OrderExpression<TEntity>(keySelector, direction));
    }
}

