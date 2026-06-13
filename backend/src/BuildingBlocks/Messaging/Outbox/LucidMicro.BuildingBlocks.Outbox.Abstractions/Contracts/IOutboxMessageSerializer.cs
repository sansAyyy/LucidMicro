using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;

public interface IOutboxMessageSerializer
{
    OutboxMessage Serialize<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent;
}
