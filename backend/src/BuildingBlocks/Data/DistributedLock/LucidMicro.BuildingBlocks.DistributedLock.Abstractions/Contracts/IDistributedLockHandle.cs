namespace LucidMicro.BuildingBlocks.DistributedLock.Abstractions.Contracts;

public interface IDistributedLockHandle : IAsyncDisposable
{
    string Key { get; }
}
