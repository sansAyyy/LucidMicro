using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class InMemoryAdminUserRepository : IRepository<AdminUser, Guid>
{
    private readonly List<AdminUser> _items;

    public InMemoryAdminUserRepository(IEnumerable<AdminUser> items)
    {
        _items = items.ToList();
    }

    public IReadOnlyList<AdminUser> Items => _items;

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(adminUser => adminUser.Id == id));
    }

    public Task<AdminUser?> FirstOrDefaultAsync(
        ISpecification<AdminUser> specification,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).FirstOrDefault());
    }

    public Task<IReadOnlyList<AdminUser>> ListAsync(
        ISpecification<AdminUser>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AdminUser>>(ApplySpecification(specification).ToArray());
    }

    public Task<PageResult<AdminUser>> PageAsync(
        ISpecification<AdminUser>? specification,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        var totalCount = query.Count();
        var items = query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.Take)
            .ToArray();

        return Task.FromResult(new PageResult<AdminUser>(
            items,
            totalCount,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize));
    }

    public Task<int> CountAsync(
        ISpecification<AdminUser>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).Count());
    }

    public Task<bool> AnyAsync(
        ISpecification<AdminUser>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplySpecification(specification).Any());
    }

    public Task AddAsync(AdminUser entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);

        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<AdminUser> entities, CancellationToken cancellationToken = default)
    {
        _items.AddRange(entities);

        return Task.CompletedTask;
    }

    public void Update(AdminUser entity)
    {
    }

    public void Remove(AdminUser entity)
    {
        _items.Remove(entity);
    }

    private IQueryable<AdminUser> ApplySpecification(ISpecification<AdminUser>? specification)
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

    private static IQueryable<AdminUser> ApplyOrdering(
        IQueryable<AdminUser> query,
        IReadOnlyList<OrderExpression<AdminUser>> orderExpressions)
    {
        IOrderedQueryable<AdminUser>? orderedQuery = null;

        foreach (var orderExpression in orderExpressions)
        {
            orderedQuery = orderedQuery is null
                ? ApplyFirstOrdering(query, orderExpression)
                : ApplyThenOrdering(orderedQuery, orderExpression);
        }

        return orderedQuery ?? query;
    }

    private static IOrderedQueryable<AdminUser> ApplyFirstOrdering(
        IQueryable<AdminUser> query,
        OrderExpression<AdminUser> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.OrderBy(orderExpression.KeySelector)
            : query.OrderByDescending(orderExpression.KeySelector);
    }

    private static IOrderedQueryable<AdminUser> ApplyThenOrdering(
        IOrderedQueryable<AdminUser> query,
        OrderExpression<AdminUser> orderExpression)
    {
        return orderExpression.Direction == OrderDirection.Ascending
            ? query.ThenBy(orderExpression.KeySelector)
            : query.ThenByDescending(orderExpression.KeySelector);
    }
}
