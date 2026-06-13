using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Inbox.Core;
using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Specifications;
using LucidMicro.Services.Notification.Application.Abstractions;
using LucidMicro.Services.Notification.Application.DependencyInjection;
using LucidMicro.Services.Notification.Application.Features.Notifications.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.Dtos.Requests;
using LucidMicro.Services.Notification.Application.Features.Notifications.Services;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.Services.Notification.Application.Tests;

public sealed class NotificationApplicationServiceTests
{
    [Fact]
    public async Task CreateAsync_AddsNotificationSendsItAndSavesChanges()
    {
        var sentAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        var repository = new InMemoryNotificationRepository();
        var sender = new TestNotificationSender(sentAt);
        var unitOfWork = new TestUnitOfWork();
        var service = new NotificationApplicationService(repository, sender, unitOfWork);
        var request = new CreateNotificationRequest(
            "admin@example.com",
            NotificationChannel.InApp,
            "Welcome",
            "Welcome to LucidMicro.");

        var result = await service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        var message = Assert.Single(repository.Items);
        Assert.Same(message, sender.SentMessages.Single());
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(message.Id, result.Value.Id);
        Assert.Equal(NotificationStatus.Sent, result.Value.Status);
        Assert.Equal(sentAt, result.Value.SentAt);
        Assert.Null(result.Value.FailedAt);
        Assert.Null(result.Value.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_SavesFailedNotification_WhenSenderMarksMessageAsFailed()
    {
        var failedAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        var repository = new InMemoryNotificationRepository();
        var sender = new TestNotificationSender(failedAt, "send failed");
        var unitOfWork = new TestUnitOfWork();
        var service = new NotificationApplicationService(repository, sender, unitOfWork);
        var request = new CreateNotificationRequest(
            "admin@example.com",
            NotificationChannel.InApp,
            "Welcome",
            "Welcome to LucidMicro.");

        var result = await service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        var message = Assert.Single(repository.Items);
        Assert.Same(message, sender.SentMessages.Single());
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(NotificationStatus.Failed, result.Value.Status);
        Assert.Null(result.Value.SentAt);
        Assert.Equal(failedAt, result.Value.FailedAt);
        Assert.Equal("send failed", result.Value.FailureReason);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        var service = CreateService(out var repository);
        var id = Guid.NewGuid();

        var result = await service.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Notification.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedNotification_WhenNotificationExists()
    {
        var message = NotificationMessage.Create(
            Guid.NewGuid(),
            "admin@example.com",
            NotificationChannel.InApp,
            "Welcome",
            "Welcome to LucidMicro.");
        var service = CreateService(out var repository);
        await repository.AddAsync(message);

        var result = await service.GetByIdAsync(message.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(message.Id, result.Value.Id);
        Assert.Equal(message.Recipient, result.Value.Recipient);
        Assert.Equal(message.Channel, result.Value.Channel);
        Assert.Equal(message.Subject, result.Value.Subject);
        Assert.Equal(message.Content, result.Value.Content);
    }

    [Fact]
    public async Task GetListAsync_ReturnsPagedNotifications_WithNewestFirst()
    {
        var oldMessage = CreateMessage("old@example.com");
        var newMessage = CreateMessage("new@example.com");
        oldMessage.MarkCreated(new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero));
        newMessage.MarkCreated(new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero));
        var service = CreateService(out var repository);
        await repository.AddRangeAsync([oldMessage, newMessage]);

        var result = await service.GetListAsync(new GetNotificationsRequest
        {
            PageNumber = 1,
            PageSize = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(1, result.Value.PageSize);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(newMessage.Id, item.Id);
        Assert.Equal("new@example.com", item.Recipient);
    }

    [Fact]
    public async Task GetListAsync_FiltersNotificationsByChannel()
    {
        var smsMessage = CreateMessage("sms@example.com", NotificationChannel.Sms);
        var inAppMessage = CreateMessage("in-app@example.com", NotificationChannel.InApp);
        var service = CreateService(out var repository);
        await repository.AddRangeAsync([smsMessage, inAppMessage]);

        var result = await service.GetListAsync(new GetNotificationsRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Channel = "Sms"
        });

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(smsMessage.Id, item.Id);
        Assert.Equal(NotificationChannel.Sms, item.Channel);
    }

    [Fact]
    public async Task GetListAsync_FiltersNotificationsByKeyword()
    {
        var matchedMessage = NotificationMessage.Create(
            Guid.NewGuid(),
            "sms@example.com",
            NotificationChannel.Sms,
            "Login code",
            "Your code is 123456.");
        var unmatchedMessage = NotificationMessage.Create(
            Guid.NewGuid(),
            "admin@example.com",
            NotificationChannel.InApp,
            "Welcome",
            "Hello.");
        var service = CreateService(out var repository);
        await repository.AddRangeAsync([matchedMessage, unmatchedMessage]);

        var result = await service.GetListAsync(new GetNotificationsRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Keyword = "code"
        });

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(matchedMessage.Id, item.Id);
    }

    [Fact]
    public async Task GetListAsync_FiltersNotificationsByDeliveredDateRange()
    {
        var matchedMessage = CreateMessage("matched@example.com");
        matchedMessage.MarkSent(new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero));
        var failedMessage = CreateMessage("failed@example.com");
        failedMessage.MarkFailed(new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero), "send failed");
        var pendingMessage = CreateMessage("pending@example.com");
        var service = CreateService(out var repository);
        await repository.AddRangeAsync([matchedMessage, failedMessage, pendingMessage]);

        var result = await service.GetListAsync(new GetNotificationsRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SentFrom = new DateOnly(2026, 5, 28),
            SentTo = new DateOnly(2026, 5, 28)
        });

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(matchedMessage.Id, item.Id);
    }

    [Fact]
    public async Task GetListAsync_ReturnsValidationError_WhenChannelIsInvalid()
    {
        var service = CreateService(out _);

        var result = await service.GetListAsync(new GetNotificationsRequest
        {
            Channel = "Unknown"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Notification.Validation", result.Error.Code);
    }

    [Fact]
    public void AddNotificationApplication_RegistersNotificationApplicationService()
    {
        var services = new ServiceCollection();

        services.AddNotificationApplication();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(INotificationApplicationService)
                       && service.ImplementationType == typeof(NotificationApplicationService));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInboxMessageProcessor)
                       && service.ImplementationType == typeof(DefaultInboxMessageProcessor));
    }

    private static NotificationApplicationService CreateService(out InMemoryNotificationRepository repository)
    {
        repository = new InMemoryNotificationRepository();
        return new NotificationApplicationService(
            repository,
            new TestNotificationSender(DateTimeOffset.Parse("2026-05-26T00:00:00+00:00")),
            new TestUnitOfWork());
    }

    private static NotificationMessage CreateMessage(
        string recipient,
        NotificationChannel channel = NotificationChannel.InApp)
    {
        return NotificationMessage.Create(
            Guid.NewGuid(),
            recipient,
            channel,
            "Welcome",
            "Welcome to LucidMicro.");
    }

    private sealed class TestNotificationSender : INotificationSender
    {
        private readonly DateTimeOffset _sentAt;
        private readonly string? _failureReason;

        public TestNotificationSender(DateTimeOffset sentAt, string? failureReason = null)
        {
            _sentAt = sentAt;
            _failureReason = failureReason;
        }

        public List<NotificationMessage> SentMessages { get; } = [];

        public Task SendAsync(
            NotificationMessage message,
            CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            if (_failureReason is not null)
            {
                message.MarkFailed(_sentAt, _failureReason);
                return Task.CompletedTask;
            }

            message.MarkSent(_sentAt);
            return Task.CompletedTask;
        }
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class InMemoryNotificationRepository : IRepository<NotificationMessage, Guid>
    {
        private readonly List<NotificationMessage> _items = [];

        public IReadOnlyList<NotificationMessage> Items => _items;

        public Task<NotificationMessage?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.FirstOrDefault(message => message.Id == id));
        }

        public Task<NotificationMessage?> FirstOrDefaultAsync(
            ISpecification<NotificationMessage> specification,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.AsQueryable().FirstOrDefault());
        }

        public Task<IReadOnlyList<NotificationMessage>> ListAsync(
            ISpecification<NotificationMessage>? specification = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<NotificationMessage>>(_items.ToArray());
        }

        public Task<PageResult<NotificationMessage>> PageAsync(
            ISpecification<NotificationMessage>? specification,
            PageRequest pageRequest,
            CancellationToken cancellationToken = default)
        {
            var query = ApplySpecification(_items.AsQueryable(), specification);
            var items = query
                .OrderByDescending(message => message.CreatedAt)
                .Skip(pageRequest.Skip)
                .Take(pageRequest.Take)
                .ToArray();

            return Task.FromResult(new PageResult<NotificationMessage>(
                items,
                query.Count(),
                pageRequest.NormalizedPageNumber,
                pageRequest.NormalizedPageSize));
        }

        public Task<int> CountAsync(
            ISpecification<NotificationMessage>? specification = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.Count);
        }

        public Task<bool> AnyAsync(
            ISpecification<NotificationMessage>? specification = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.Count > 0);
        }

        public Task AddAsync(
            NotificationMessage entity,
            CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(
            IEnumerable<NotificationMessage> entities,
            CancellationToken cancellationToken = default)
        {
            _items.AddRange(entities);
            return Task.CompletedTask;
        }

        public void Update(NotificationMessage entity)
        {
        }

        public void Remove(NotificationMessage entity)
        {
            _items.Remove(entity);
        }

        private static IQueryable<NotificationMessage> ApplySpecification(
            IQueryable<NotificationMessage> query,
            ISpecification<NotificationMessage>? specification)
        {
            if (specification?.Criteria is null)
            {
                return query;
            }

            return query.Where(specification.Criteria);
        }
    }
}
