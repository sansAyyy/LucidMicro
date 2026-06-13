namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;

public sealed record RabbitMqConsumerFailureOptions
{
    public bool RequeueOnFailure { get; init; }
}
