using LucidMicro.Services.Notification.Application.Abstractions;
using LucidMicro.Services.Notification.Domain.Entities.NotificationMessages;
using LucidMicro.Services.Notification.Domain.Enums;
using LucidMicro.Services.Notification.Infrastructure.DependencyInjection;
using LucidMicro.Services.Notification.Infrastructure.Sending;
using LucidMicro.Tests.Shared.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LucidMicro.Services.Notification.Infrastructure.Tests;

public sealed class NotificationSenderTests
{
    [Fact]
    public async Task SendAsync_MarksMessageAsSent_WhenChannelSenderSucceeds()
    {
        var now = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        var message = CreateMessage();
        var sender = new DefaultNotificationSender(
            [new TestNotificationChannelSender(NotificationChannel.InApp)],
            new TestTimeProvider(now),
            NullLogger<DefaultNotificationSender>.Instance);

        await sender.SendAsync(message);

        Assert.Equal(NotificationStatus.Sent, message.Status);
        Assert.Equal(now, message.SentAt);
        Assert.Null(message.FailedAt);
        Assert.Null(message.FailureReason);
    }

    [Fact]
    public async Task SendAsync_MarksMessageAsFailed_WhenChannelSenderFails()
    {
        var now = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        var message = CreateMessage();
        var sender = new DefaultNotificationSender(
            [
                new TestNotificationChannelSender(
                    NotificationChannel.InApp,
                    new InvalidOperationException("send failed"))
            ],
            new TestTimeProvider(now),
            NullLogger<DefaultNotificationSender>.Instance);

        await sender.SendAsync(message);

        Assert.Equal(NotificationStatus.Failed, message.Status);
        Assert.Null(message.SentAt);
        Assert.Equal(now, message.FailedAt);
        Assert.Equal("send failed", message.FailureReason);
    }

    [Fact]
    public async Task SendAsync_MarksMessageAsFailed_WhenChannelSenderIsMissing()
    {
        var now = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        var message = CreateMessage();
        var sender = new DefaultNotificationSender(
            [],
            new TestTimeProvider(now),
            NullLogger<DefaultNotificationSender>.Instance);

        await sender.SendAsync(message);

        Assert.Equal(NotificationStatus.Failed, message.Status);
        Assert.Null(message.SentAt);
        Assert.Equal(now, message.FailedAt);
        Assert.Contains("InApp", message.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public void AddNotificationInfrastructure_RegistersNotificationSenders()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Notification"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Lucid:ServiceDiscovery:Consul:Address"] = "http://consul:8500",
                ["Lucid:ServiceDiscovery:Consul:RequestTimeoutSeconds"] = "3",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceName"] = "notification",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceId"] = "notification-test",
                ["Lucid:ServiceDiscovery:Consul:Registration:Address"] = "localhost",
                ["Lucid:ServiceDiscovery:Consul:Registration:Port"] = "49853",
                ["Lucid:EventBus:RabbitMQ:ConnectionString"] = "amqp://guest:guest@localhost:5672/",
                ["Lucid:EventBus:RabbitMQ:ExchangeName"] = "lucid.events",
                ["Authentication:Jwt:Issuer"] = "LucidMicro.Identity",
                ["Authentication:Jwt:Audience"] = "LucidMicro.Admin",
                ["Authentication:Jwt:RefreshAudience"] = "LucidMicro.Admin.Refresh",
                ["Authentication:Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes"
            })
            .Build();

        services.AddNotificationInfrastructure(configuration);

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(INotificationSender)
                       && service.ImplementationType == typeof(DefaultNotificationSender));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(INotificationChannelSender)
                       && service.ImplementationType == typeof(LogNotificationChannelSender));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(INotificationChannelSender)
                       && service.ImplementationType == typeof(LogSmsNotificationChannelSender));
    }

    private static NotificationMessage CreateMessage()
    {
        return NotificationMessage.Create(
            Guid.NewGuid(),
            "admin@example.com",
            NotificationChannel.InApp,
            "Welcome",
            "Welcome to LucidMicro.");
    }

    private sealed class TestNotificationChannelSender : INotificationChannelSender
    {
        private readonly Exception? _exception;

        public TestNotificationChannelSender(
            NotificationChannel channel,
            Exception? exception = null)
        {
            Channel = channel;
            _exception = exception;
        }

        public NotificationChannel Channel { get; }

        public Task SendAsync(
            NotificationMessage message,
            CancellationToken cancellationToken = default)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.CompletedTask;
        }
    }
}
