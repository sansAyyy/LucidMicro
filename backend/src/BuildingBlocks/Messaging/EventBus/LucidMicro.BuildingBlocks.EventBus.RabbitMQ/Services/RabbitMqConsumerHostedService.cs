using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Services;

internal sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private const ushort PrefetchCount = 16;

    private readonly RabbitMqEventBusOptions _options;
    private readonly IReadOnlyList<RabbitMqConsumerRegistration> _registrations;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private readonly RabbitMqConsumerMessageDispatcher _dispatcher;
    private readonly ConnectionFactory _connectionFactory;
    private readonly List<IChannel> _channels = [];
    private IConnection? _connection;

    public RabbitMqConsumerHostedService(
        RabbitMqEventBusOptions options,
        IEnumerable<RabbitMqConsumerRegistration> registrations,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment hostEnvironment,
        ILogger<RabbitMqConsumerHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        options.Validate();

        _options = options;
        _registrations = registrations.ToArray();
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _dispatcher = new RabbitMqConsumerMessageDispatcher(scopeFactory);
        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(options.ConnectionString)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_registrations.Count == 0)
        {
            return;
        }

        try
        {
            _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);

            foreach (var group in _registrations.GroupBy(
                         registration => GetQueueName(registration, _hostEnvironment.ApplicationName)))
            {
                await StartConsumerAsync(group.Key, group.ToArray(), stoppingToken);
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "RabbitMQ consumer is stopping. Exchange: {ExchangeName}, ConsumerCount: {ConsumerCount}",
                _options.ExchangeName,
                _registrations.Count);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "RabbitMQ consumer failed to start. Exchange: {ExchangeName}, ConsumerCount: {ConsumerCount}",
                _options.ExchangeName,
                _registrations.Count);

            throw;
        }
        finally
        {
            await DisposeRabbitMqAsync();
        }
    }

    internal static string GetQueueName(
        RabbitMqConsumerRegistration registration,
        string applicationName)
    {
        if (!string.IsNullOrWhiteSpace(registration.QueueName))
        {
            return registration.QueueName;
        }

        var normalizedApplicationName = NormalizeQueueNamePart(applicationName);
        var normalizedConsumerName = NormalizeQueueNamePart(registration.HandlerType.Name);

        return $"{normalizedApplicationName}.{normalizedConsumerName}";
    }

    private async Task StartConsumerAsync(
        string queueName,
        IReadOnlyCollection<RabbitMqConsumerRegistration> registrations,
        CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("RabbitMQ connection has not been created.");
        }

        var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        _channels.Add(channel);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        foreach (var registration in registrations)
        {
            await channel.QueueBindAsync(
                queue: queueName,
                exchange: _options.ExchangeName,
                routingKey: registration.BindingKey,
                arguments: null,
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation(
            "RabbitMQ consumer started. Exchange: {ExchangeName}, Queue: {QueueName}, BindingKeys: {BindingKeys}",
            _options.ExchangeName,
            queueName,
            registrations.Select(registration => registration.BindingKey).ToArray());

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);

        var registrationsByType = registrations.ToDictionary(
            registration => registration.BindingKey,
            StringComparer.Ordinal);
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += (_, args) => HandleMessageAsync(
            channel,
            registrationsByType,
            args);

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        IReadOnlyDictionary<string, RabbitMqConsumerRegistration> registrationsByType,
        BasicDeliverEventArgs args)
    {
        try
        {
            var registration = await _dispatcher.DispatchAsync(
                registrationsByType,
                args.Body,
                args.CancellationToken);

            if (registration is null)
            {
                await channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: args.CancellationToken);

                return;
            }

            await channel.BasicAckAsync(
                args.DeliveryTag,
                multiple: false,
                cancellationToken: args.CancellationToken);
        }
        catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "RabbitMQ message handling was canceled. EventType: {EventType}, DeliveryTag: {DeliveryTag}",
                "unknown",
                args.DeliveryTag);
        }
        catch (RabbitMqConsumerDispatchException exception)
        {
            _logger.LogError(
                exception.InnerException ?? exception,
                "RabbitMQ message handling failed. EventType: {EventType}, DeliveryTag: {DeliveryTag}, Requeue: {Requeue}",
                exception.Registration.BindingKey,
                args.DeliveryTag,
                exception.Registration.RequeueOnFailure);

            await channel.BasicNackAsync(
                args.DeliveryTag,
                multiple: false,
                requeue: exception.Registration.RequeueOnFailure,
                cancellationToken: args.CancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "RabbitMQ message handling failed. EventType: {EventType}, DeliveryTag: {DeliveryTag}, Requeue: {Requeue}",
                "unknown",
                args.DeliveryTag,
                false);

            await channel.BasicNackAsync(
                args.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: args.CancellationToken);
        }
    }

    private async ValueTask DisposeRabbitMqAsync()
    {
        foreach (var channel in _channels)
        {
            await channel.DisposeAsync();
        }

        _channels.Clear();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private static string NormalizeQueueNamePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "application";
        }

        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '.')
            .ToArray();
        var normalized = new string(chars);

        while (normalized.Contains("..", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("..", ".", StringComparison.Ordinal);
        }

        var normalizedValue = normalized.Trim('.');

        return string.IsNullOrWhiteSpace(normalizedValue) ? "application" : normalizedValue;
    }
}
