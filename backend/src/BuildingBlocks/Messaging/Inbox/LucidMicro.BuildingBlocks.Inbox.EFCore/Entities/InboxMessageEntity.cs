using LucidMicro.BuildingBlocks.Inbox.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Inbox.EFCore.Entities;

public sealed class InboxMessageEntity
{
    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static InboxMessageEntity FromMessage(InboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new InboxMessageEntity
        {
            Id = message.Id,
            Type = message.Type,
            ProcessedAt = message.ProcessedAt,
            CreatedAt = message.CreatedAt
        };
    }

    public InboxMessage ToMessage()
    {
        return new InboxMessage
        {
            Id = Id,
            Type = Type,
            ProcessedAt = ProcessedAt,
            CreatedAt = CreatedAt
        };
    }
}
