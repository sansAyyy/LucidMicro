using System.Text.Json;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.Contracts.Notification;
using LucidMicro.Contracts.Notification.Http.Requests;
using LucidMicro.Contracts.Notification.Http.Responses;
using LucidMicro.Contracts.Notification.IntegrationEvents;

namespace LucidMicro.Contracts.Tests;

public sealed class NotificationContractTests
{
    [Fact]
    public void NotificationSendRequestedIntegrationEvent_UsesStableEventName()
    {
        var eventName = IntegrationEventNameResolver.Resolve<NotificationSendRequestedIntegrationEvent>();

        Assert.Equal("notification.send-requested.v1", eventName);
    }

    [Fact]
    public void Create_NormalizesValues()
    {
        var integrationEvent = NotificationSendRequestedIntegrationEvent.Create(
            " admin@example.com ",
            $" {NotificationChannels.InApp} ",
            " Welcome ",
            " Welcome to LucidMicro. ");

        Assert.Equal("admin@example.com", integrationEvent.Recipient);
        Assert.Equal(NotificationChannels.InApp, integrationEvent.Channel);
        Assert.Equal("Welcome", integrationEvent.Subject);
        Assert.Equal("Welcome to LucidMicro.", integrationEvent.Content);
    }

    [Fact]
    public void Create_UsesNullSubject_WhenSubjectIsWhiteSpace()
    {
        var integrationEvent = NotificationSendRequestedIntegrationEvent.Create(
            "admin@example.com",
            NotificationChannels.InApp,
            " ",
            "Welcome to LucidMicro.");

        Assert.Null(integrationEvent.Subject);
    }

    [Fact]
    public void SendNotificationRequest_UsesStableJsonPropertyNames()
    {
        var request = new SendNotificationRequest(
            "admin@example.com",
            NotificationChannels.InApp,
            "Welcome",
            "Hello");

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var document = JsonDocument.Parse(json);

        Assert.Equal("admin@example.com", document.RootElement.GetProperty("recipient").GetString());
        Assert.Equal(NotificationChannels.InApp, document.RootElement.GetProperty("channel").GetString());
        Assert.Equal("Welcome", document.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Hello", document.RootElement.GetProperty("content").GetString());
    }

    [Fact]
    public void NotificationResponse_UsesStableJsonPropertyNames()
    {
        var response = new NotificationResponse(
            Guid.Parse("3377e91e-6fd2-4e2c-a2f9-80ac9f9acb01"),
            "admin@example.com",
            NotificationChannels.InApp,
            "Welcome",
            "Hello",
            NotificationStatuses.Sent,
            new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero),
            null,
            null);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        var document = JsonDocument.Parse(json);

        Assert.Equal(response.Id, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("admin@example.com", document.RootElement.GetProperty("recipient").GetString());
        Assert.Equal(NotificationChannels.InApp, document.RootElement.GetProperty("channel").GetString());
        Assert.Equal(NotificationStatuses.Sent, document.RootElement.GetProperty("status").GetString());
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
