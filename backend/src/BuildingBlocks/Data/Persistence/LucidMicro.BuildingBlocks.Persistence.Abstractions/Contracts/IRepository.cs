using LucidMicro.BuildingBlocks.Domain.Core.Entities;

namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;

public interface IRepository<TEntity, TId> : IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}

