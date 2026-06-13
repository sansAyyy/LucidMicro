using LucidMicro.BuildingBlocks.Inbox.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class InboxMessageTests
{
    [Fact]
    public void InboxMessage_CapturesProcessedEventIdentity()
    {
        var processedAt = DateTimeOffset.Parse("2026-05-27T00:00:00+00:00");
        var message = new InboxMessage
        {
            Id = Guid.Parse("e357010b-a90b-4183-aecb-2371d1fe8b6f"),
            Type = "notification.send-requested.v1",
            ProcessedAt = processedAt
        };

        Assert.Equal(Guid.Parse("e357010b-a90b-4183-aecb-2371d1fe8b6f"), message.Id);
        Assert.Equal("notification.send-requested.v1", message.Type);
        Assert.Equal(processedAt, message.ProcessedAt);
    }
}
