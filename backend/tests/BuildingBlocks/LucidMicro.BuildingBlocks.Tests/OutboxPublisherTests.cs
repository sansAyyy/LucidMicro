using LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.Core;
using LucidMicro.BuildingBlocks.Outbox.Core.DependencyInjection;
using LucidMicro.BuildingBlocks.Outbox.Core.Options;
using LucidMicro.Tests.Shared.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class OutboxPublisherTests
{
    [Fact]
    public async Task PublishPendingAsync_PublishesEnvelopeAndMarksMessageAsPublished()
    {
        var now = DateTimeOffset.Parse("2026-05-26T10:00:00+00:00");
        var message = CreateMessage();
        var store = new TestOutboxMessageStore([message]);
        var envelopePublisher = new TestEnvelopePublisher();
        var publisher = new DefaultOutboxPublisher(
            store,
            envelopePublisher,
            new TestTimeProvider(now),
            new OutboxPublisherOptions(),
            NullLogger<DefaultOutboxPublisher>.Instance);

        await publisher.PublishPendingAsync();

        var envelope = Assert.Single(envelopePublisher.PublishedEnvelopes);
        Assert.Equal(message.Id, envelope.Id);
        Assert.Equal(message.Type, envelope.Type);
        Assert.Equal(message.OccurredAt, envelope.OccurredAt);
        Assert.Equal(message.TraceParent, envelope.TraceParent);
        Assert.Equal(message.TraceState, envelope.TraceState);
        Assert.Equal(message.Payload, envelope.Payload);
        Assert.Equal(now, store.PublishedMessages[message.Id]);
        Assert.Empty(store.FailedMessages);
        Assert.Equal(1, store.SaveChangesCount);
    }

    [Fact]
    public async Task PublishPendingAsync_MarksMessageAsFailed_WhenPublishFails()
    {
        var message = CreateMessage();
        var store = new TestOutboxMessageStore([message]);
        var envelopePublisher = new TestEnvelopePublisher
        {
            Exception = new InvalidOperationException("publish failed")
        };
        var publisher = new DefaultOutboxPublisher(
            store,
            envelopePublisher,
            new TestTimeProvider(DateTimeOffset.Parse("2026-05-26T10:00:00+00:00")),
            new OutboxPublisherOptions(),
            NullLogger<DefaultOutboxPublisher>.Instance);

        await publisher.PublishPendingAsync();

        Assert.Empty(envelopePublisher.PublishedEnvelopes);
        Assert.Empty(store.PublishedMessages);
        var failure = store.FailedMessages[message.Id];
        Assert.Equal("publish failed", failure.Error);
        Assert.Equal(DateTimeOffset.Parse("2026-05-26T10:00:30+00:00"), failure.NextRetryAt);
        Assert.Null(failure.DeadAt);
        Assert.Equal(1, store.SaveChangesCount);
    }

    [Fact]
    public async Task PublishPendingAsync_MarksMessageAsDead_WhenMaxRetryCountIsReached()
    {
        var now = DateTimeOffset.Parse("2026-05-26T10:00:00+00:00");
        var message = CreateMessage() with
        {
            FailureCount = 1
        };
        var store = new TestOutboxMessageStore([message]);
        var envelopePublisher = new TestEnvelopePublisher
        {
            Exception = new InvalidOperationException("publish failed")
        };
        var publisher = new DefaultOutboxPublisher(
            store,
            envelopePublisher,
            new TestTimeProvider(now),
            new OutboxPublisherOptions
            {
                MaxRetryCount = 2
            },
            NullLogger<DefaultOutboxPublisher>.Instance);

        await publisher.PublishPendingAsync();

        var failure = store.FailedMessages[message.Id];
        Assert.Equal("publish failed", failure.Error);
        Assert.Null(failure.NextRetryAt);
        Assert.Equal(now, failure.DeadAt);
        Assert.Equal(1, store.SaveChangesCount);
    }

    [Fact]
    public void AddLucidOutboxPublisher_RegistersPublisherAndTimeProvider()
    {
        var services = new ServiceCollection();

        services.AddLucidOutboxPublisher();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IOutboxPublisher)
                       && service.ImplementationType == typeof(DefaultOutboxPublisher));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(TimeProvider)
                       && service.ImplementationInstance == TimeProvider.System);
    }

    private static OutboxMessage CreateMessage()
    {
        return new OutboxMessage
        {
            Id = Guid.Parse("7d0c96e5-df71-48ac-9d60-069e1a301d05"),
            Type = "identity.admin-user.created.v1",
            OccurredAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            TraceParent = "00-7d0c96e5df7148ac9d60069e1a301d05-7d0c96e5df7148ac-01",
            TraceState = "vendor=value",
            Payload = """{"name":"admin"}"""
        };
    }

    private sealed class TestOutboxMessageStore : IOutboxMessageStore
    {
        private readonly IReadOnlyList<OutboxMessage> _messages;

        public TestOutboxMessageStore(IReadOnlyList<OutboxMessage> messages)
        {
            _messages = messages;
        }

        public Dictionary<Guid, DateTimeOffset> PublishedMessages { get; } = [];

        public Dictionary<Guid, FailedMessage> FailedMessages { get; } = [];

        public int SaveChangesCount { get; private set; }

        public Task AddAsync(
            OutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(
                _messages.Take(maxCount).ToArray());
        }

        public Task MarkAsPublishedAsync(
            Guid messageId,
            DateTimeOffset publishedAt,
            CancellationToken cancellationToken = default)
        {
            PublishedMessages[messageId] = publishedAt;
            return Task.CompletedTask;
        }

        public Task MarkAsFailedAsync(
            Guid messageId,
            string error,
            DateTimeOffset? nextRetryAt,
            DateTimeOffset? deadAt,
            CancellationToken cancellationToken = default)
        {
            FailedMessages[messageId] = new FailedMessage(error, nextRetryAt, deadAt);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }

    private sealed record FailedMessage(
        string Error,
        DateTimeOffset? NextRetryAt,
        DateTimeOffset? DeadAt);

    private sealed class TestEnvelopePublisher : IIntegrationEventEnvelopePublisher
    {
        public List<IntegrationEventEnvelope> PublishedEnvelopes { get; } = [];

        public Exception? Exception { get; init; }

        public Task PublishAsync(
            IntegrationEventEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            PublishedEnvelopes.Add(envelope);
            return Task.CompletedTask;
        }
    }
}
