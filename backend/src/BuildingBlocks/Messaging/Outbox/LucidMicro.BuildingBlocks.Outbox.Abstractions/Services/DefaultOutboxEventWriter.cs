using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;

namespace LucidMicro.BuildingBlocks.Outbox.Abstractions.Services;

public sealed class DefaultOutboxEventWriter : IOutboxEventWriter
{
    private readonly IOutboxMessageSerializer _serializer;
    private readonly IOutboxMessageStore _store;

    public DefaultOutboxEventWriter(
        IOutboxMessageStore store,
        IOutboxMessageSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(serializer);

        _store = store;
        _serializer = serializer;
    }

    public Task AddAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return _store.AddAsync(_serializer.Serialize(integrationEvent), cancellationToken);
    }
}
