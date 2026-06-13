using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Inbox.Core.Contracts;

public interface IInboxMessageProcessor
{
    Task ProcessAsync<TEvent>(
        TEvent integrationEvent,
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
