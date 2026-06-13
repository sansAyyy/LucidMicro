using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;

namespace LucidMicro.BuildingBlocks.Inbox.Core;

public sealed class DefaultInboxMessageProcessor : IInboxMessageProcessor
{
    private readonly IInboxMessageStore _inbox;
    private readonly IInboxProcessingTransaction _transaction;

    public DefaultInboxMessageProcessor(
        IInboxMessageStore inbox,
        IInboxProcessingTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(inbox);
        ArgumentNullException.ThrowIfNull(transaction);

        _inbox = inbox;
        _transaction = transaction;
    }

    public async Task ProcessAsync<TEvent>(
        TEvent integrationEvent,
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (await _inbox.HasProcessedAsync(integrationEvent.Id, cancellationToken))
        {
            return;
        }

        await _transaction.ExecuteAsync(
            async ct =>
            {
                await handler(integrationEvent, ct);
                await _inbox.MarkProcessedAsync(integrationEvent, ct);
                await _inbox.SaveChangesAsync(ct);
            },
            cancellationToken);
    }
}
