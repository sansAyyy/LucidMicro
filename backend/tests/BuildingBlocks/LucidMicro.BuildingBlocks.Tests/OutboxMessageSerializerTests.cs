using System.Diagnostics;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Serialization;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Services;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class OutboxMessageSerializerTests
{
    [Fact]
    public void Serialize_CreatesOutboxMessage()
    {
        var integrationEvent = new TestIntegrationEvent
        {
            Id = Guid.Parse("7d0c96e5-df71-48ac-9d60-069e1a301d05"),
            OccurredAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            Name = "admin"
        };
        var serializer = new SystemTextJsonOutboxMessageSerializer();

        var message = serializer.Serialize(integrationEvent);

        Assert.Equal(integrationEvent.Id, message.Id);
        Assert.Equal("identity.admin-user.created.v1", message.Type);
        Assert.Equal(integrationEvent.OccurredAt, message.OccurredAt);
        Assert.Null(message.TraceParent);
        Assert.Null(message.TraceState);
        Assert.Contains("\"name\":\"admin\"", message.Payload);
        Assert.Null(message.PublishedAt);
        Assert.Equal(0, message.FailureCount);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void Serialize_CapturesTraceContext()
    {
        using var activity = new Activity("test")
            .SetIdFormat(ActivityIdFormat.W3C);
        activity.TraceStateString = "vendor=value";
        activity.Start();
        var serializer = new SystemTextJsonOutboxMessageSerializer();

        var message = serializer.Serialize(new TestIntegrationEvent());

        Assert.Equal(activity.Id, message.TraceParent);
        Assert.Equal(activity.TraceStateString, message.TraceState);
    }

    [Fact]
    public async Task OutboxEventWriter_SerializesEventAndAddsMessageToStore()
    {
        var store = new TestOutboxMessageStore();
        var writer = new DefaultOutboxEventWriter(store, new SystemTextJsonOutboxMessageSerializer());
        var integrationEvent = new TestIntegrationEvent
        {
            Name = "admin"
        };

        await writer.AddAsync(integrationEvent);

        var message = Assert.Single(store.Items);
        Assert.Equal(integrationEvent.Id, message.Id);
        Assert.Equal("identity.admin-user.created.v1", message.Type);
        Assert.Contains("\"name\":\"admin\"", message.Payload);
    }

    [IntegrationEventName("identity.admin-user.created.v1")]
    private sealed record TestIntegrationEvent : IntegrationEvent
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestOutboxMessageStore : IOutboxMessageStore
    {
        private readonly List<OutboxMessage> _items = [];

        public IReadOnlyList<OutboxMessage> Items => _items;

        public Task AddAsync(
            OutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            _items.Add(message);

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(_items.Take(maxCount).ToArray());
        }

        public Task MarkAsPublishedAsync(
            Guid messageId,
            DateTimeOffset publishedAt,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task MarkAsFailedAsync(
            Guid messageId,
            string error,
            DateTimeOffset? nextRetryAt,
            DateTimeOffset? deadAt,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }
}
