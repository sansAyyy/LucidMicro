using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LucidMicro.Contracts.Notification;
using LucidMicro.Contracts.Notification.Http.Requests;
using LucidMicro.Services.Identity.Application.ExternalServices.Notifications;
using LucidMicro.Services.Identity.Infrastructure.DependencyInjection;
using LucidMicro.Services.Identity.Infrastructure.ExternalServices.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.Services.Identity.Infrastructure.Tests;

public sealed class NotificationClientTests
{
    [Fact]
    public void AddIdentityInfrastructure_RegistersNotificationClient()
    {
        var services = new ServiceCollection();

        services.AddIdentityInfrastructure(CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<INotificationClient>();

        Assert.IsType<NotificationClient>(client);
    }

    [Fact]
    public async Task SendAsync_PostsNotificationRequest()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var client = CreateClient(handler);
        var request = new SendNotificationRequest(
            "admin@example.com",
            NotificationChannels.InApp,
            "Welcome",
            "Hello");

        var result = await client.SendAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(new Uri("http://notification/internal/notifications"), handler.RequestUri);
        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("admin@example.com", body.RootElement.GetProperty("recipient").GetString());
        Assert.Equal(NotificationChannels.InApp, body.RootElement.GetProperty("channel").GetString());
        Assert.Equal("Welcome", body.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Hello", body.RootElement.GetProperty("content").GetString());
    }

    [Fact]
    public async Task SendAsync_ReturnsFailure_WhenNotificationServiceReturnsNonSuccess()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        var result = await client.SendAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.Notification.RequestFailed", result.Error.Code);
        Assert.Equal("Notification service returned HTTP status code 400.", result.Error.Message);
    }

    [Fact]
    public async Task SendAsync_IncludesDownstreamProblemDetails_WhenNotificationServiceReturnsProblemDetails()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new
            {
                title = "notification channel is invalid.",
                code = "Notification.Validation"
            })
        });
        var client = CreateClient(handler);

        var result = await client.SendAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.Notification.RequestFailed", result.Error.Code);
        Assert.Equal(
            "Notification service returned HTTP status code 400. Downstream error code: Notification.Validation. notification channel is invalid.",
            result.Error.Message);
    }

    [Fact]
    public async Task SendAsync_IgnoresNonJsonErrorBody_WhenNotificationServiceReturnsNonSuccess()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("plain text failure")
        });
        var client = CreateClient(handler);

        var result = await client.SendAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.Notification.RequestFailed", result.Error.Code);
        Assert.Equal("Notification service returned HTTP status code 500.", result.Error.Message);
    }

    [Fact]
    public async Task SendAsync_ReturnsFailure_WhenNotificationServiceIsUnavailable()
    {
        var handler = new TestHttpMessageHandler(_ => throw new HttpRequestException("Connection failed."));
        var client = CreateClient(handler);

        var result = await client.SendAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.Notification.Unavailable", result.Error.Code);
    }

    private static NotificationClient CreateClient(HttpMessageHandler handler)
    {
        return new NotificationClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://notification")
        });
    }

    private static SendNotificationRequest CreateRequest()
    {
        return new SendNotificationRequest(
            "admin@example.com",
            NotificationChannels.InApp,
            "Welcome",
            "Hello");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Lucid:Caching:Redis:ConnectionString"] = "localhost:6379",
                ["Lucid:Identity:SmsLogin:CodeTtlSeconds"] = "300",
                ["Lucid:Identity:SmsLogin:SendIntervalSeconds"] = "60",
                ["Lucid:Identity:SmsLogin:AttemptTtlSeconds"] = "300",
                ["Lucid:Identity:SmsLogin:MaxAttempts"] = "5",
                ["Lucid:Resilience:Http:Enabled"] = "false",
                ["Lucid:ServiceDiscovery:Consul:Address"] = "http://consul:8500",
                ["Lucid:ServiceDiscovery:Consul:RequestTimeoutSeconds"] = "3",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceName"] = "identity",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceId"] = "identity-test",
                ["Lucid:ServiceDiscovery:Consul:Registration:Address"] = "localhost",
                ["Lucid:ServiceDiscovery:Consul:Registration:Port"] = "49753",
                ["Lucid:EventBus:RabbitMQ:ConnectionString"] = "amqp://guest:guest@localhost:5672/",
                ["Lucid:EventBus:RabbitMQ:ExchangeName"] = "lucid.events",
                ["Authentication:Jwt:Issuer"] = "LucidMicro.Identity",
                ["Authentication:Jwt:Audience"] = "LucidMicro.Admin",
                ["Authentication:Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes"
            })
            .Build();
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public HttpMethod? RequestMethod { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _handler(request);
        }
    }

}
