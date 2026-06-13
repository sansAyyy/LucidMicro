using System.Diagnostics;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Internal;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using RabbitMQ.Client;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Services;

public sealed class RabbitMqEventBus : IEventBus, IIntegrationEventEnvelopePublisher, IAsyncDisposable
{
    private const string ContentType = "application/json";

    private readonly RabbitMqEventBusOptions _options;
    private readonly RabbitMqIntegrationEventSerializer _serializer = new();
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqEventBus(RabbitMqEventBusOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _options = options;
        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(options.ConnectionString)
        };
    }

    public async Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        using var activity = RabbitMqEventBusDiagnostics.ActivitySource.StartActivity(
            $"publish {IntegrationEventNameResolver.Resolve<TEvent>()}",
            ActivityKind.Producer);

        try
        {
            var envelope = _serializer.CreateEnvelope(integrationEvent);
            await PublishEnvelopeAsync(envelope, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            RabbitMqEventBusDiagnostics.SetError(activity, exception);
            throw;
        }
    }

    public async Task PublishAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        using var activity = RabbitMqEventBusDiagnostics.ActivitySource.StartActivity(
            $"publish {envelope.Type}",
            ActivityKind.Producer);

        try
        {
            await PublishEnvelopeAsync(envelope, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            RabbitMqEventBusDiagnostics.SetError(activity, exception);
            throw;
        }
    }

    private async Task PublishEnvelopeAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var body = _serializer.SerializeEnvelope(envelope);
        var exchangeName = GetExchangeName();
        var routingKey = envelope.Type;
        RabbitMqEventBusDiagnostics.EnrichProducerActivity(
            Activity.Current,
            envelope,
            exchangeName,
            routingKey);

        var connection = await GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            ContentType = ContentType,
            MessageId = envelope.Id.ToString("N"),
            Type = envelope.Type,
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection?.IsOpen == true)
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private string GetExchangeName()
    {
        return _options.ExchangeName;
    }
}
