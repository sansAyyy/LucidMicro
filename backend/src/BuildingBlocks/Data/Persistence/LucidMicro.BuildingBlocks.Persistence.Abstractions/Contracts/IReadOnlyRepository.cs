using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;

namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;

public interface IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default);

    Task<PageResult<TEntity>> PageAsync(
        ISpecification<TEntity>? specification,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default);
}

