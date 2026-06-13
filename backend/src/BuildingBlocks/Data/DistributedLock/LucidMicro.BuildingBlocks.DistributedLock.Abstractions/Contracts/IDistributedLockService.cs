namespace LucidMicro.BuildingBlocks.DistributedLock.Abstractions.Contracts;

public interface IDistributedLockService
{
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<IDistributedLockHandle?> WaitAcquireAsync(
        string key,
        TimeSpan expiry,
        TimeSpan waitTimeout,
        CancellationToken cancellationToken = default);
}
