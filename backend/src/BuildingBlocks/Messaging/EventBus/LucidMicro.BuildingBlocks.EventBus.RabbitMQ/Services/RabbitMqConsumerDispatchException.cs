using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Services;

internal sealed class RabbitMqConsumerDispatchException : Exception
{
    public RabbitMqConsumerDispatchException(
        RabbitMqConsumerRegistration registration,
        Exception innerException)
        : base("RabbitMQ consumer dispatch failed.", innerException)
    {
        ArgumentNullException.ThrowIfNull(registration);

        Registration = registration;
    }

    public RabbitMqConsumerRegistration Registration { get; }
}
