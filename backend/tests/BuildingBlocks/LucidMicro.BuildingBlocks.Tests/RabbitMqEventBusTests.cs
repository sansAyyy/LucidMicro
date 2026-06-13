using System.Diagnostics;
using System.Text.Json;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.DependencyInjection;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Internal;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class RabbitMqEventBusTests
{
    [Fact]
    public void FromConfiguration_BindsOptions()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionString"] = "amqp://guest:guest@localhost:5672/",
            ["ExchangeName"] = "lucid.identity.events"
        };

        var options = RabbitMqEventBusOptions.FromConfiguration(configuration);

        Assert.Equal("amqp://guest:guest@localhost:5672/", options.ConnectionString);
        Assert.Equal("lucid.identity.events", options.ExchangeName);
    }

    [Fact]
    public void FromConfiguration_UsesDefaultExchangeName()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionString"] = "amqp://guest:guest@localhost:5672/"
        };

        var options = RabbitMqEventBusOptions.FromConfiguration(configuration);

        Assert.Equal("lucid.events", options.ExchangeName);
    }

    [Fact]
    public void Validate_Throws_WhenConnectionStringIsMissing()
    {
        var options = new RabbitMqEventBusOptions();

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenConnectionStringIsInvalid()
    {
        var options = new RabbitMqEventBusOptions
        {
            ConnectionString = "localhost",
            ExchangeName = "lucid.events"
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenExchangeNameIsMissing()
    {
        var options = new RabbitMqEventBusOptions
        {
            ConnectionString = "amqp://guest:guest@localhost:5672/",
            ExchangeName = ""
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public async Task AddLucidRabbitMqEventBus_RegistersEventBusAndEnvelopePublisher()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager
        {
            ["Lucid:EventBus:RabbitMQ:ConnectionString"] = "amqp://guest:guest@localhost:5672/"
        };

        services.AddLucidRabbitMqEventBus(
            configuration.GetRequiredSection(RabbitMqEventBusOptions.ConfigurationSectionName));

        await using var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        var envelopePublisher = serviceProvider.GetRequiredService<IIntegrationEventEnvelopePublisher>();

        Assert.IsType<RabbitMqEventBus>(eventBus);
        Assert.Same(eventBus, envelopePublisher);
    }

    [Fact]
    public void AddLucidRabbitMqConsumer_RegistersHandlerAndConsumerMetadata()
    {
        var services = new ServiceCollection();

        services.AddLucidRabbitMqConsumer<TestIntegrationEvent, TestIntegrationEventHandler>();

        var handlerDescriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IIntegrationEventHandler<TestIntegrationEvent>));
        var registrationDescriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(RabbitMqConsumerRegistration));
        var registration = Assert.IsType<RabbitMqConsumerRegistration>(
            registrationDescriptor.ImplementationInstance);

        Assert.Equal(typeof(TestIntegrationEventHandler), handlerDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, handlerDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, registrationDescriptor.Lifetime);
        Assert.Equal(typeof(TestIntegrationEvent), registration.EventType);
        Assert.Equal(typeof(TestIntegrationEventHandler), registration.HandlerType);
        Assert.Equal(nameof(TestIntegrationEvent), registration.BindingKey);
        Assert.Null(registration.QueueName);
        Assert.False(registration.FailureOptions.RequeueOnFailure);
        Assert.False(registration.RequeueOnFailure);
    }

    [Fact]
    public void AddLucidRabbitMqConsumer_UsesIntegrationEventName()
    {
        var services = new ServiceCollection();

        services.AddLucidRabbitMqConsumer<NamedIntegrationEvent, NamedIntegrationEventHandler>();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(RabbitMqConsumerRegistration));
        var registration = Assert.IsType<RabbitMqConsumerRegistration>(descriptor.ImplementationInstance);

        Assert.Equal("identity.admin-user.created.v1", registration.BindingKey);
    }

    [Fact]
    public void AddLucidRabbitMqConsumer_RegistersHostedServiceOnce()
    {
        var services = new ServiceCollection();

        services.AddLucidRabbitMqConsumer<TestIntegrationEvent, TestIntegrationEventHandler>();
        services.AddLucidRabbitMqConsumer<AnotherIntegrationEvent, AnotherIntegrationEventHandler>();

        var descriptors = services
            .Where(service => service.ServiceType == typeof(IHostedService)
                              && service.ImplementationType == typeof(RabbitMqConsumerHostedService))
            .ToArray();

        Assert.Single(descriptors);
    }

    [Fact]
    public void AddLucidRabbitMqConsumer_Throws_WhenConsumerIsRegisteredTwice()
    {
        var services = new ServiceCollection();

        services.AddLucidRabbitMqConsumer<TestIntegrationEvent, TestIntegrationEventHandler>();

        Assert.Throws<InvalidOperationException>(
            () => services.AddLucidRabbitMqConsumer<TestIntegrationEvent, TestIntegrationEventHandler>());
    }

    [Fact]
    public void AddLucidRabbitMqConsumer_UsesExplicitQueueNameAndRequeueOption()
    {
        var services = new ServiceCollection();

        services.AddLucidRabbitMqConsumer<TestIntegrationEvent, TestIntegrationEventHandler>(
            queueName: "identity.audit-events",
            requeueOnFailure: true);

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(RabbitMqConsumerRegistration));
        var registration = Assert.IsType<RabbitMqConsumerRegistration>(descriptor.ImplementationInstance);

        Assert.Equal("identity.audit-events", registration.QueueName);
        Assert.True(registration.FailureOptions.RequeueOnFailure);
        Assert.True(registration.RequeueOnFailure);
    }

    [Fact]
    public void ConsumerHostedService_GeneratesDefaultQueueName()
    {
        var registration = RabbitMqConsumerRegistration.Create<TestIntegrationEvent, TestIntegrationEventHandler>();

        var queueName = RabbitMqConsumerHostedService.GetQueueName(
            registration,
            "LucidMicro.Services.Identity.Api");

        Assert.Equal("lucidmicro.services.identity.api.testintegrationeventhandler", queueName);
    }

    [Fact]
    public async Task ConsumerMessageDispatcher_DispatchesToRegisteredHandler()
    {
        RecordingIntegrationEventHandler.HandledEvents.Clear();
        var services = new ServiceCollection();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, RecordingIntegrationEventHandler>();
        await using var serviceProvider = services.BuildServiceProvider();
        var registration = RabbitMqConsumerRegistration.Create<TestIntegrationEvent, RecordingIntegrationEventHandler>();
        var dispatcher = CreateDispatcher(serviceProvider);
        var body = CreateMessageBody(new TestIntegrationEvent
        {
            Id = Guid.Parse("ee65ef7d-3f49-47fe-b6ea-1497067fb4c1"),
            Name = "admin"
        });

        var handledRegistration = await dispatcher.DispatchAsync(CreateRegistrations(registration), body);

        Assert.Same(registration, handledRegistration);
        var handledEvent = Assert.Single(RecordingIntegrationEventHandler.HandledEvents);
        Assert.Equal("admin", handledEvent.Name);
    }

    [Fact]
    public async Task ConsumerMessageDispatcher_StartsConsumerActivityFromEnvelopeTraceContext()
    {
        var stoppedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RabbitMqEventBusDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => stoppedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, RecordingIntegrationEventHandler>();
        await using var serviceProvider = services.BuildServiceProvider();
        var registration = RabbitMqConsumerRegistration.Create<TestIntegrationEvent, RecordingIntegrationEventHandler>();
        var dispatcher = CreateDispatcher(serviceProvider);
        using var producerActivity = new Activity("producer").SetIdFormat(ActivityIdFormat.W3C);
        producerActivity.TraceStateString = "vendor=value";
        producerActivity.Start();
        var body = CreateMessageBody(new TestIntegrationEvent
        {
            Name = "admin"
        });
        var expectedTraceId = producerActivity.TraceId;
        var expectedParentSpanId = producerActivity.SpanId;
        producerActivity.Stop();

        await dispatcher.DispatchAsync(CreateRegistrations(registration), body);

        var consumerActivity = Assert.Single(
            stoppedActivities,
            activity => activity.OperationName == "consume TestIntegrationEvent");
        Assert.Equal(expectedTraceId, consumerActivity.TraceId);
        Assert.Equal(expectedParentSpanId, consumerActivity.ParentSpanId);
        Assert.Equal("rabbitmq", GetActivityTag<string>(consumerActivity, "messaging.system"));
        Assert.Equal("process", GetActivityTag<string>(consumerActivity, "messaging.operation"));
        Assert.Equal("TestIntegrationEvent", GetActivityTag<string>(consumerActivity, "messaging.message.type"));
        Assert.Equal(
            typeof(RecordingIntegrationEventHandler).FullName,
            GetActivityTag<string>(consumerActivity, "lucid.consumer.handler"));
    }

    [Fact]
    public async Task ConsumerMessageDispatcher_ReturnsUnknownType_WhenEventTypeIsNotRegistered()
    {
        var services = new ServiceCollection();
        await using var serviceProvider = services.BuildServiceProvider();
        var registration = RabbitMqConsumerRegistration.Create<TestIntegrationEvent, TestIntegrationEventHandler>();
        var dispatcher = CreateDispatcher(serviceProvider);
        var body = CreateMessageBody(new AnotherIntegrationEvent());

        var handledRegistration = await dispatcher.DispatchAsync(CreateRegistrations(registration), body);

        Assert.Null(handledRegistration);
    }

    [Fact]
    public async Task ConsumerMessageDispatcher_ThrowsDispatchException_WhenHandlerFails()
    {
        var stoppedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RabbitMqEventBusDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => stoppedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, FailingIntegrationEventHandler>();
        await using var serviceProvider = services.BuildServiceProvider();
        var registration = RabbitMqConsumerRegistration.Create<TestIntegrationEvent, FailingIntegrationEventHandler>(
            requeueOnFailure: true);
        var dispatcher = CreateDispatcher(serviceProvider);
        var body = CreateMessageBody(new TestIntegrationEvent());

        var exception = await Assert.ThrowsAsync<RabbitMqConsumerDispatchException>(
            () => dispatcher.DispatchAsync(CreateRegistrations(registration), body));

        Assert.Same(registration, exception.Registration);
        Assert.IsType<InvalidOperationException>(exception.InnerException);

        var consumerActivity = Assert.Single(
            stoppedActivities,
            activity => activity.OperationName == "consume TestIntegrationEvent");
        Assert.Equal(ActivityStatusCode.Error, consumerActivity.Status);
        Assert.Contains(
            consumerActivity.Events,
            activityEvent => activityEvent.Name == "exception");
    }

    [Fact]
    public async Task ConsumerMessageDispatcher_PropagatesCancellation_WhenStoppingTokenIsCanceled()
    {
        var services = new ServiceCollection();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, CancelingIntegrationEventHandler>();
        await using var serviceProvider = services.BuildServiceProvider();
        var registration = RabbitMqConsumerRegistration.Create<TestIntegrationEvent, CancelingIntegrationEventHandler>();
        var dispatcher = CreateDispatcher(serviceProvider);
        var body = CreateMessageBody(new TestIntegrationEvent());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => dispatcher.DispatchAsync(CreateRegistrations(registration), body, cts.Token));
    }

    [Fact]
    public void Serializer_CreatesEnvelope()
    {
        var integrationEvent = new TestIntegrationEvent
        {
            Id = Guid.Parse("7d0c96e5-df71-48ac-9d60-069e1a301d05"),
            OccurredAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            Name = "admin"
        };
        var serializer = new RabbitMqIntegrationEventSerializer();

        var envelope = serializer.CreateEnvelope(integrationEvent);

        Assert.Equal(integrationEvent.Id, envelope.Id);
        Assert.Equal(nameof(TestIntegrationEvent), envelope.Type);
        Assert.Equal(integrationEvent.OccurredAt, envelope.OccurredAt);
        Assert.Null(envelope.TraceParent);
        Assert.Null(envelope.TraceState);
        Assert.Contains("\"name\":\"admin\"", envelope.Payload);
    }

    [Fact]
    public void Serializer_CreatesEnvelopeWithTraceContext()
    {
        using var activity = new Activity("test")
            .SetIdFormat(ActivityIdFormat.W3C);
        activity.TraceStateString = "vendor=value";
        activity.Start();
        var integrationEvent = new TestIntegrationEvent
        {
            Name = "admin"
        };
        var serializer = new RabbitMqIntegrationEventSerializer();

        var envelope = serializer.CreateEnvelope(integrationEvent);

        Assert.Equal(activity.Id, envelope.TraceParent);
        Assert.Equal(activity.TraceStateString, envelope.TraceState);
    }

    [Fact]
    public void Serializer_CreatesEnvelopeWithIntegrationEventName()
    {
        var integrationEvent = new NamedIntegrationEvent();
        var serializer = new RabbitMqIntegrationEventSerializer();

        var envelope = serializer.CreateEnvelope(integrationEvent);

        Assert.Equal("identity.admin-user.created.v1", envelope.Type);
    }

    [Fact]
    public void Serializer_SerializesEnvelope()
    {
        var integrationEvent = new TestIntegrationEvent
        {
            Id = Guid.Parse("7d0c96e5-df71-48ac-9d60-069e1a301d05"),
            OccurredAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            Name = "admin"
        };
        var serializer = new RabbitMqIntegrationEventSerializer();
        var envelope = serializer.CreateEnvelope(integrationEvent);

        var body = serializer.SerializeEnvelope(envelope);
        var serializedEnvelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
            body.Span,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(serializedEnvelope);
        Assert.Equal(envelope.Id, serializedEnvelope.Id);
        Assert.Equal(envelope.Type, serializedEnvelope.Type);
        Assert.Equal(envelope.Payload, serializedEnvelope.Payload);
    }

    [Fact]
    public void Serializer_DeserializesEnvelopeAndEvent()
    {
        var integrationEvent = new TestIntegrationEvent
        {
            Id = Guid.Parse("7d0c96e5-df71-48ac-9d60-069e1a301d05"),
            OccurredAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            Name = "admin"
        };
        var serializer = new RabbitMqIntegrationEventSerializer();
        var envelope = serializer.CreateEnvelope(integrationEvent);
        var body = serializer.SerializeEnvelope(envelope);

        var deserializedEnvelope = serializer.DeserializeEnvelope(body);
        var deserializedEvent = Assert.IsType<TestIntegrationEvent>(
            serializer.DeserializeEvent(deserializedEnvelope, typeof(TestIntegrationEvent)));

        Assert.Equal(envelope.Id, deserializedEnvelope.Id);
        Assert.Equal(envelope.Type, deserializedEnvelope.Type);
        Assert.Equal(integrationEvent.Id, deserializedEvent.Id);
        Assert.Equal(integrationEvent.OccurredAt, deserializedEvent.OccurredAt);
        Assert.Equal(integrationEvent.Name, deserializedEvent.Name);
    }

    private sealed record TestIntegrationEvent : IntegrationEvent
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed record AnotherIntegrationEvent : IntegrationEvent;

    [IntegrationEventName("identity.admin-user.created.v1")]
    private sealed record NamedIntegrationEvent : IntegrationEvent;

    private sealed class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task HandleAsync(
            TestIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public static List<TestIntegrationEvent> HandledEvents { get; } = [];

        public Task HandleAsync(
            TestIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(integrationEvent);

            return Task.CompletedTask;
        }
    }

    private sealed class FailingIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task HandleAsync(
            TestIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException(new InvalidOperationException("Handler failed."));
        }
    }

    private sealed class CancelingIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task HandleAsync(
            TestIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    private sealed class AnotherIntegrationEventHandler : IIntegrationEventHandler<AnotherIntegrationEvent>
    {
        public Task HandleAsync(
            AnotherIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NamedIntegrationEventHandler : IIntegrationEventHandler<NamedIntegrationEvent>
    {
        public Task HandleAsync(
            NamedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private static RabbitMqConsumerMessageDispatcher CreateDispatcher(IServiceProvider serviceProvider)
    {
        return new RabbitMqConsumerMessageDispatcher(
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    private static IReadOnlyDictionary<string, RabbitMqConsumerRegistration> CreateRegistrations(
        params RabbitMqConsumerRegistration[] registrations)
    {
        return registrations.ToDictionary(
            registration => registration.BindingKey,
            StringComparer.Ordinal);
    }

    private static ReadOnlyMemory<byte> CreateMessageBody<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent
    {
        var serializer = new RabbitMqIntegrationEventSerializer();

        return serializer.SerializeEnvelope(serializer.CreateEnvelope(integrationEvent));
    }

    private static T? GetActivityTag<T>(Activity activity, string key)
    {
        return activity.TagObjects
            .Where(tag => tag.Key == key)
            .Select(tag => tag.Value)
            .OfType<T>()
            .SingleOrDefault();
    }
}
