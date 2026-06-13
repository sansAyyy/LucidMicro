using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Core;
using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class InboxMessageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_InvokesHandlerAndMarksEvent_WhenEventWasNotProcessed()
    {
        var inbox = new TestInboxMessageStore();
        var transaction = new TestInboxProcessingTransaction();
        var processor = new DefaultInboxMessageProcessor(inbox, transaction);
        var integrationEvent = new TestIntegrationEvent();
        var handlerCallCount = 0;

        await processor.ProcessAsync(
            integrationEvent,
            (message, _) =>
            {
                Assert.Same(integrationEvent, message);
                handlerCallCount++;

                return Task.CompletedTask;
            });

        Assert.Equal(1, handlerCallCount);
        Assert.Same(integrationEvent, Assert.Single(inbox.MarkedEvents));
        Assert.Equal(1, inbox.SaveChangesCount);
        Assert.Equal(1, transaction.ExecuteCount);
    }

    [Fact]
    public async Task ProcessAsync_SkipsHandlerAndMarking_WhenEventWasProcessed()
    {
        var integrationEvent = new TestIntegrationEvent();
        var inbox = new TestInboxMessageStore();
        inbox.ProcessedIds.Add(integrationEvent.Id);
        var transaction = new TestInboxProcessingTransaction();
        var processor = new DefaultInboxMessageProcessor(inbox, transaction);
        var handlerCallCount = 0;

        await processor.ProcessAsync(
            integrationEvent,
            (_, _) =>
            {
                handlerCallCount++;

                return Task.CompletedTask;
            });

        Assert.Equal(0, handlerCallCount);
        Assert.Empty(inbox.MarkedEvents);
        Assert.Equal(0, inbox.SaveChangesCount);
        Assert.Equal(0, transaction.ExecuteCount);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotMarkEvent_WhenHandlerThrows()
    {
        var inbox = new TestInboxMessageStore();
        var transaction = new TestInboxProcessingTransaction();
        var processor = new DefaultInboxMessageProcessor(inbox, transaction);
        var integrationEvent = new TestIntegrationEvent();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => processor.ProcessAsync<TestIntegrationEvent>(
                integrationEvent,
                (_, _) => throw new InvalidOperationException("Failed.")));

        Assert.Empty(inbox.MarkedEvents);
        Assert.Equal(0, inbox.SaveChangesCount);
        Assert.Equal(1, transaction.ExecuteCount);
    }

    [Fact]
    public void AddLucidInboxProcessor_RegistersDefaultProcessor()
    {
        var services = new ServiceCollection();

        services.AddLucidInboxProcessor();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInboxMessageProcessor)
                       && service.ImplementationType == typeof(DefaultInboxMessageProcessor));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInboxProcessingTransaction)
                       && service.ImplementationType == typeof(NoOpInboxProcessingTransaction));
    }

    [IntegrationEventName("test.event.v1")]
    private sealed record TestIntegrationEvent : IntegrationEvent;

    private sealed class TestInboxMessageStore : IInboxMessageStore
    {
        public HashSet<Guid> ProcessedIds { get; } = [];

        public List<IntegrationEvent> MarkedEvents { get; } = [];

        public int SaveChangesCount { get; private set; }

        public Task<bool> HasProcessedAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ProcessedIds.Contains(id));
        }

        public Task MarkProcessedAsync<TEvent>(
            TEvent integrationEvent,
            CancellationToken cancellationToken = default)
            where TEvent : IntegrationEvent
        {
            ProcessedIds.Add(integrationEvent.Id);
            MarkedEvents.Add(integrationEvent);

            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;

            return Task.FromResult(1);
        }
    }

    private sealed class TestInboxProcessingTransaction : IInboxProcessingTransaction
    {
        public int ExecuteCount { get; private set; }

        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            ExecuteCount++;

            await operation(cancellationToken);
        }
    }
}
