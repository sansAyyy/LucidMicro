using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;

public interface IInboxMessageStore
{
    Task<bool> HasProcessedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
