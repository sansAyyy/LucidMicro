namespace LucidMicro.BuildingBlocks.Inbox.Core.Contracts;

public interface IInboxProcessingTransaction
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
