using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;

        return Task.FromResult(1);
    }
}
