namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

