using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Core;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Contracts.Notification.IntegrationEvents;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Responses;
using LucidMicro.Services.Notification.Application.Features.Notifications.IntegrationEvents;
using LucidMicro.Services.Notification.Domain.Enums;

namespace LucidMicro.Services.Notification.Application.Tests;

public sealed class NotificationSendRequestedIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesNotificationRequest()
    {
        var notifications = new TestNotificationApplicationService();
        var inbox = new TestInboxMessageStore();
        var handler = CreateHandler(notifications, inbox);
        var integrationEvent = new NotificationSendRequestedIntegrationEvent
        {
            Recipient = "admin@example.com",
            Channel = "InApp",
            Subject = "Welcome",
            Content = "Welcome to LucidMicro."
        };

        await handler.HandleAsync(integrationEvent);

        var request = Assert.Single(notifications.Requests);
        Assert.Equal("admin@example.com", request.Recipient);
        Assert.Equal(NotificationChannel.InApp, request.Channel);
        Assert.Equal("Welcome", request.Subject);
        Assert.Equal("Welcome to LucidMicro.", request.Content);

        Assert.Same(integrationEvent, Assert.Single(inbox.MarkedEvents));
        Assert.Equal(1, inbox.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_DoesNothing_WhenEventWasAlreadyProcessed()
    {
        var notifications = new TestNotificationApplicationService();
        var inbox = new TestInboxMessageStore();
        var integrationEvent = new NotificationSendRequestedIntegrationEvent
        {
            Recipient = "admin@example.com",
            Channel = "InApp",
            Subject = "Welcome",
            Content = "Welcome to LucidMicro."
        };
        inbox.ProcessedIds.Add(integrationEvent.Id);
        var handler = CreateHandler(notifications, inbox);

        await handler.HandleAsync(integrationEvent);

        Assert.Empty(notifications.Requests);
        Assert.Empty(inbox.MarkedEvents);
        Assert.Equal(0, inbox.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenChannelIsUnsupported()
    {
        var handler = CreateHandler();
        var integrationEvent = new NotificationSendRequestedIntegrationEvent
        {
            Recipient = "admin@example.com",
            Channel = "Unknown",
            Content = "Welcome to LucidMicro."
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(integrationEvent));

        Assert.Contains("Unsupported notification channel", exception.Message, StringComparison.Ordinal);
    }

    private static NotificationSendRequestedIntegrationEventHandler CreateHandler(
        TestNotificationApplicationService? notifications = null,
        TestInboxMessageStore? inbox = null)
    {
        return new NotificationSendRequestedIntegrationEventHandler(
            notifications ?? new TestNotificationApplicationService(),
            new DefaultInboxMessageProcessor(
                inbox ?? new TestInboxMessageStore(),
                new NoOpInboxProcessingTransaction()));
    }

    private sealed class TestNotificationApplicationService : INotificationApplicationService
    {
        public List<CreateNotificationRequest> Requests { get; } = [];

        public Task<Result<PageResult<NotificationResponse>>> GetListAsync(
            GetNotificationsRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result<NotificationResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Result<NotificationResponse>> CreateAsync(
            CreateNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            return Task.FromResult(Result<NotificationResponse>.Success(new NotificationResponse(
                Guid.NewGuid(),
                request.Recipient ?? string.Empty,
                request.Channel,
                request.Subject,
                request.Content ?? string.Empty,
                NotificationStatus.Sent,
                DateTimeOffset.UtcNow,
                null,
                null)));
        }
    }

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
}
