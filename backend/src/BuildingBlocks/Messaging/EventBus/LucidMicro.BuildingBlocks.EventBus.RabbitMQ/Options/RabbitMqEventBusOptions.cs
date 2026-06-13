using Microsoft.Extensions.Configuration;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;

public sealed class RabbitMqEventBusOptions
{
    public const string ConfigurationSectionName = "Lucid:EventBus:RabbitMQ";

    public string ConnectionString { get; set; } = string.Empty;

    public string ExchangeName { get; set; } = "lucid.events";

    public static RabbitMqEventBusOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new RabbitMqEventBusOptions
        {
            ConnectionString = configuration[nameof(ConnectionString)] ?? string.Empty,
            ExchangeName = configuration[nameof(ExchangeName)] ?? "lucid.events"
        };
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(ExchangeName);

        if (!Uri.TryCreate(ConnectionString, UriKind.Absolute, out _))
        {
            throw new ArgumentException("ConnectionString must be an absolute URI.", nameof(ConnectionString));
        }
    }
}
