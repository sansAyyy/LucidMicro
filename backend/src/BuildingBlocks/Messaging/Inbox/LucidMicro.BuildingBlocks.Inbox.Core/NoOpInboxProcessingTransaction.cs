using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;

namespace LucidMicro.BuildingBlocks.Inbox.Core;

public sealed class NoOpInboxProcessingTransaction : IInboxProcessingTransaction
{
    public Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return operation(cancellationToken);
    }
}
