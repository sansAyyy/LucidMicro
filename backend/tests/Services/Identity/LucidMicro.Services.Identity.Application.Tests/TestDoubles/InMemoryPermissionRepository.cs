using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class InMemoryPermissionRepository : IReadOnlyRepository<Permission, Guid>
{
    private readonly List<Permission> _items;

    public InMemoryPermissionRepository(IEnumerable<Permission> items)
    {
        _items = items.ToList();
    }

    public IReadOnlyList<Permission> Items => _items;

    public Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(permission => permission.Id == id));
    }

    public Task<Permission?> FirstOrDefaultAsync(
        ISpecification<Permission> specification,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).FirstOrDefault());
    }

    public Task<IReadOnlyList<Permission>> ListAsync(
        ISpecification<Permission>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Permission>>(ApplySpecification(specification).ToArray());
    }

    public Task<PageResult<Permission>> PageAsync(
        ISpecification<Permission>? specification,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        var totalCount = query.Count();
        var items = query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.Take)
            .ToArray();

        return Task.FromResult(new PageResult<Permission>(
            items,
            totalCount,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize));
    }

    public Task<int> CountAsync(
        ISpecification<Permission>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).Count());
    }

    public Task<bool> AnyAsync(
        ISpecification<Permission>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).Any());
    }

    private IQueryable<Permission> ApplySpecification(ISpecification<Permission>? specification)
    {
        var query = _items.AsQueryable();

        if (specification?.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification is not null)
        {
            query = ApplyOrdering(query, specification.OrderExpressions);
        }

        return query;
    }

    private static IQueryable<Permission> ApplyOrdering(
        IQueryable<Permission> query,
        IReadOnlyList<OrderExpression<Permission>> orderExpressions)
    {
        IOrderedQueryable<Permission>? orderedQuery = null;

        foreach (var orderExpression in orderExpressions)
        {
            orderedQuery = orderedQuery is null
                ? ApplyFirstOrdering(query, orderExpression)
                : ApplyThenOrdering(orderedQuery, orderExpression);
        }

        return orderedQuery ?? query;
    }

    private static IOrderedQueryable<Permission> ApplyFirstOrdering(
        IQueryable<Permission> query,
        OrderExpression<Permission> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.OrderBy(orderExpression.KeySelector)
            : query.OrderByDescending(orderExpression.KeySelector);
    }

    private static IOrderedQueryable<Permission> ApplyThenOrdering(
        IOrderedQueryable<Permission> query,
        OrderExpression<Permission> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.ThenBy(orderExpression.KeySelector)
            : query.ThenByDescending(orderExpression.KeySelector);
    }
}
