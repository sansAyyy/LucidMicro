namespace LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;

public interface IOutboxPublisher
{
    Task PublishPendingAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default);
}
