using System.Diagnostics;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Internal;

internal static class RabbitMqEventBusDiagnostics
{
    public const string ActivitySourceName = "LucidMicro.EventBus.RabbitMQ";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static void EnrichProducerActivity(
        Activity? activity,
        IntegrationEventEnvelope envelope,
        string exchangeName,
        string routingKey)
    {
        if (activity is null)
        {
            return;
        }

        SetCommonMessagingTags(activity, envelope.Type);
        activity.SetTag("messaging.operation", "publish");
        activity.SetTag("messaging.destination.name", exchangeName);
        activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
    }

    public static void EnrichConsumerActivity(
        Activity? activity,
        IntegrationEventEnvelope envelope,
        RabbitMqConsumerRegistration? registration)
    {
        if (activity is null)
        {
            return;
        }

        SetCommonMessagingTags(activity, envelope.Type);
        activity.SetTag("messaging.operation", "process");

        if (registration is not null)
        {
            activity.SetTag("lucid.consumer.handler", registration.HandlerType.FullName);
        }
    }

    public static void SetError(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                ["exception.type"] = exception.GetType().FullName,
                ["exception.message"] = exception.Message
            }));
    }

    private static void SetCommonMessagingTags(Activity activity, string messageType)
    {
        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.message.type", messageType);
    }
}
