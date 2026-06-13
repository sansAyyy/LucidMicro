using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class InMemoryRoleRepository : IRepository<Role, Guid>
{
    private readonly List<Role> _items;

    public InMemoryRoleRepository(IEnumerable<Role> items)
    {
        _items = items.ToList();
    }

    public IReadOnlyList<Role> Items => _items;

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(role => role.Id == id));
    }

    public Task<Role?> FirstOrDefaultAsync(
        ISpecification<Role> specification,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).FirstOrDefault());
    }

    public Task<IReadOnlyList<Role>> ListAsync(
        ISpecification<Role>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Role>>(ApplySpecification(specification).ToArray());
    }

    public Task<PageResult<Role>> PageAsync(
        ISpecification<Role>? specification,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        var totalCount = query.Count();
        var items = query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.Take)
            .ToArray();

        return Task.FromResult(new PageResult<Role>(
            items,
            totalCount,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize));
    }

    public Task<int> CountAsync(
        ISpecification<Role>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).Count());
    }

    public Task<bool> AnyAsync(
        ISpecification<Role>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).Any());
    }

    public Task AddAsync(Role entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);

        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Role> entities, CancellationToken cancellationToken = default)
    {
        _items.AddRange(entities);

        return Task.CompletedTask;
    }

    public void Update(Role entity)
    {
    }

    public void Remove(Role entity)
    {
        _items.Remove(entity);
    }

    private IQueryable<Role> ApplySpecification(ISpecification<Role>? specification)
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

    private static IQueryable<Role> ApplyOrdering(
        IQueryable<Role> query,
        IReadOnlyList<OrderExpression<Role>> orderExpressions)
    {
        IOrderedQueryable<Role>? orderedQuery = null;

        foreach (var orderExpression in orderExpressions)
        {
            orderedQuery = orderedQuery is null
                ? ApplyFirstOrdering(query, orderExpression)
                : ApplyThenOrdering(orderedQuery, orderExpression);
        }

        return orderedQuery ?? query;
    }

    private static IOrderedQueryable<Role> ApplyFirstOrdering(
        IQueryable<Role> query,
        OrderExpression<Role> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.OrderBy(orderExpression.KeySelector)
            : query.OrderByDescending(orderExpression.KeySelector);
    }

    private static IOrderedQueryable<Role> ApplyThenOrdering(
        IOrderedQueryable<Role> query,
        OrderExpression<Role> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.ThenBy(orderExpression.KeySelector)
            : query.ThenByDescending(orderExpression.KeySelector);
    }
}
