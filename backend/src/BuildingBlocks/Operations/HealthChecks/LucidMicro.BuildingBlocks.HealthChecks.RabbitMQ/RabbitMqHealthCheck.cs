using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqEventBusOptions _options;

    public RabbitMqHealthCheck(RabbitMqEventBusOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(_options.ConnectionString)
            };

            await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ is reachable.")
                : HealthCheckResult.Unhealthy("RabbitMQ is unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ health check failed.", exception);
        }
    }
}
