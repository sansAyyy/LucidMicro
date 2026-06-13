using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Specifications;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Repositories;

public class EfReadOnlyRepository<TEntity, TId> : IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    public EfReadOnlyRepository(DbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    protected DbContext DbContext { get; }

    protected DbSet<TEntity> DbSet { get; }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public virtual async Task<PageResult<TEntity>> PageAsync(
        ISpecification<TEntity>? specification,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pageRequest);

        var query = ApplySpecification(specification, evaluatePagination: false);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.Take)
            .ToListAsync(cancellationToken);

        return new PageResult<TEntity>(
            items,
            totalCount,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, evaluatePagination: false)
            .CountAsync(cancellationToken)
            ;
    }

    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, evaluatePagination: false)
            .AnyAsync(cancellationToken)
            ;
    }

    protected IQueryable<TEntity> ApplySpecification(
        ISpecification<TEntity>? specification,
        bool evaluatePagination = true)
    {
        return SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), specification, evaluatePagination);
    }
}

