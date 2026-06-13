using System.Diagnostics;
using System.Reflection;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Services;

internal sealed class RabbitMqConsumerMessageDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqIntegrationEventSerializer _serializer = new();

    public RabbitMqConsumerMessageDispatcher(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);

        _scopeFactory = scopeFactory;
    }

    public async Task<RabbitMqConsumerRegistration?> DispatchAsync(
        IReadOnlyDictionary<string, RabbitMqConsumerRegistration> registrationsByType,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registrationsByType);

        RabbitMqConsumerRegistration? registration = null;
        Activity? activity = null;

        try
        {
            var envelope = _serializer.DeserializeEnvelope(body);
            activity = StartConsumerActivity(envelope);
            RabbitMqEventBusDiagnostics.EnrichConsumerActivity(activity, envelope, registration);

            if (!registrationsByType.TryGetValue(envelope.Type, out registration))
            {
                return null;
            }

            RabbitMqEventBusDiagnostics.EnrichConsumerActivity(activity, envelope, registration);

            var integrationEvent = _serializer.DeserializeEvent(envelope, registration.EventType);
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(registration.EventType);
            var handler = scope.ServiceProvider.GetRequiredService(handlerType);
            var handleMethod = handlerType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync))
                ?? throw new InvalidOperationException("Integration event handler is missing HandleAsync.");
            var task = handleMethod.Invoke(
                handler,
                [integrationEvent, cancellationToken]) as Task
                ?? throw new InvalidOperationException("Integration event handler returned an invalid result.");

            await task;

            return registration;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TargetInvocationException exception)
            when (cancellationToken.IsCancellationRequested
                  && exception.InnerException is OperationCanceledException operationCanceledException)
        {
            throw operationCanceledException;
        }
        catch (Exception exception) when (registration is not null)
        {
            RabbitMqEventBusDiagnostics.SetError(activity, exception);
            throw new RabbitMqConsumerDispatchException(registration, exception);
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private static Activity? StartConsumerActivity(IntegrationEventEnvelope envelope)
    {
        var activityName = $"consume {envelope.Type}";

        if (ActivityContext.TryParse(
                envelope.TraceParent,
                envelope.TraceState,
                isRemote: true,
                out var parentContext))
        {
            return RabbitMqEventBusDiagnostics.ActivitySource.StartActivity(
                activityName,
                ActivityKind.Consumer,
                parentContext);
        }

        return RabbitMqEventBusDiagnostics.ActivitySource.StartActivity(
            activityName,
            ActivityKind.Consumer);
    }
}
