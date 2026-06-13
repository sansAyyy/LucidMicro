using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;

public sealed record RabbitMqConsumerRegistration
{
    public required Type EventType { get; init; }

    public required Type HandlerType { get; init; }

    public required string BindingKey { get; init; }

    public string? QueueName { get; init; }

    public required RabbitMqConsumerFailureOptions FailureOptions { get; init; }

    public bool RequeueOnFailure => FailureOptions.RequeueOnFailure;

    public static RabbitMqConsumerRegistration Create<TEvent, THandler>(
        string? queueName = null,
        bool requeueOnFailure = false)
        where TEvent : IntegrationEvent
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            queueName = null;
        }

        return new RabbitMqConsumerRegistration
        {
            EventType = typeof(TEvent),
            HandlerType = typeof(THandler),
            BindingKey = IntegrationEventNameResolver.Resolve<TEvent>(),
            QueueName = queueName,
            FailureOptions = new RabbitMqConsumerFailureOptions
            {
                RequeueOnFailure = requeueOnFailure
            }
        };
    }
}
