# Messaging 约定

本文档记录当前后端 Messaging / EventBus 的最小约定。

## 项目结构

当前 EventBus BuildingBlock 包含：

```text
BuildingBlocks/Messaging/EventBus/
  LucidMicro.BuildingBlocks.EventBus.Abstractions/
  LucidMicro.BuildingBlocks.EventBus.RabbitMQ/
```

暂不接入任何业务服务。

## IntegrationEvent

跨服务发布的消息统一称为 Integration Event。

```csharp
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
```

约定：

- Integration Event 用于服务边界之间的异步通信。
- Integration Event 应表达已经发生的事实，命名使用过去式，例如 `AdminUserCreatedIntegrationEvent`。
- Integration Event 不应直接暴露领域实体或 EF Core 实体。
- `Id` 是事件实例标识，用于日志、追踪和后续幂等处理。
- `OccurredAt` 表示事件产生时间，使用 UTC。

事件名约定：

- 事件名用于 envelope `Type`、RabbitMQ routing key 和 consumer binding key。
- 推荐使用稳定事件名，而不是依赖 C# 类型名。
- 稳定事件名通过 `IntegrationEventNameAttribute` 声明。
- 未声明 attribute 时，默认使用事件类型名，保持早期开发便利性。

```csharp
[IntegrationEventName("identity.admin-user.created.v1")]
public sealed record AdminUserCreatedIntegrationEvent : IntegrationEvent;
```

命名建议：

- 使用小写 kebab-case 或点分层级。
- 建议包含服务边界、业务对象、事实动作和版本号。
- 示例：`identity.admin-user.created.v1`。

共享契约约定：

- 跨服务共享的 integration event 契约放在 `backend/src/Contracts`。
- 契约项目按业务边界拆分，例如 `LucidMicro.Contracts.Notification`。
- 业务服务发布通知请求时依赖 Notification 契约，不直接引用 Notification 的 Domain、Application 或 Infrastructure。
- Notification 当前统一请求事件为 `notification.send-requested.v1`，定义在：

```text
backend/src/Contracts/LucidMicro.Contracts.Notification/
  IntegrationEvents/NotificationSendRequestedIntegrationEvent.cs
```

## IntegrationEventEnvelope

EventBus 实现对外传输时应使用统一消息信封。

```csharp
public sealed record IntegrationEventEnvelope
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public string? TraceParent { get; init; }

    public string? TraceState { get; init; }

    public required string Payload { get; init; }
}
```

约定：

- `Id` 来自 `IntegrationEvent.Id`。
- `Type` 是事件类型标识，具体命名策略由后续实现约定。
- `OccurredAt` 来自 `IntegrationEvent.OccurredAt`。
- `TraceParent` 和 `TraceState` 用于传播 W3C Trace Context。
- `Payload` 是事件内容的序列化结果。
- Envelope 是 EventBus 实现层的传输模型，业务代码优先使用具体 `IntegrationEvent` 类型。

当前 RabbitMQ 实现将 envelope 序列化到 message body，暂不把 trace context 同步写入 RabbitMQ headers。

Envelope 发布约定：

- 业务代码优先使用 `IEventBus.PublishAsync<TEvent>(...)`。
- Outbox publisher 可以使用 `IIntegrationEventEnvelopePublisher` 直接发布 `IntegrationEventEnvelope`。
- Envelope 发布不反序列化回 CLR integration event，也不依赖事件类型注册表。
- Envelope 的 `Type` 同时作为 RabbitMQ routing key。
- Envelope 发布能力属于 EventBus 实现层能力，不要求业务应用层直接依赖。

## RabbitMQ 第一版约定

EventBus 第一版 RabbitMQ 实现使用官方 `RabbitMQ.Client`，暂不引入 MassTransit、CAP 或 Wolverine。

配置节：

```json
{
  "Lucid": {
    "EventBus": {
      "RabbitMQ": {
        "ConnectionString": "amqp://guest:guest@localhost:5672/",
        "ExchangeName": "lucid.events"
      }
    }
  }
}
```

约定：

- 配置节为 `Lucid:EventBus:RabbitMQ`。
- `ExchangeName` 可配置，默认值为 `lucid.events`。
- 第一版实现发布端 `IEventBus.PublishAsync(...)` 和最小 consumer HostedService。
- RabbitMQ provider 同时注册 `IEventBus` 和 `IIntegrationEventEnvelopePublisher`，并复用同一个 `RabbitMqEventBus` 实例。
- 第一版不实现重试、死信、outbox/inbox。
- Exchange 类型使用 `topic`。
- Routing key 使用事件名；优先使用 `IntegrationEventNameAttribute`，未声明时使用 `typeof(TEvent).Name`。
- 消息 body 使用 `IntegrationEventEnvelope` 的 JSON。
- RabbitMQ 实现内部使用 `System.Text.Json` 序列化，暂不拆独立 serialization BuildingBlock。
- RabbitMQ 发布端复用 connection，每次发布创建短生命周期 channel。

RabbitMQ message properties 第一版约定：

- `ContentType`：`application/json`。
- `MessageId`：`IntegrationEventEnvelope.Id`。
- `Type`：`IntegrationEventEnvelope.Type`。

RabbitMQ trace context 第一版约定：

- 发布消息时，从 `Activity.Current` 读取 W3C `TraceParent` 和 `TraceState`，写入 `IntegrationEventEnvelope`。
- 消费消息时，从 envelope 还原 parent context，并创建 consumer activity。
- OpenTelemetry BuildingBlock 默认监听 `LucidMicro.EventBus.RabbitMQ` activity source。
- 第一版不传播 baggage，也不把 trace context 写入 RabbitMQ headers。

RabbitMQ ready check 通过 `LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ` 注册：

```csharp
services.AddLucidRabbitMqHealthCheck();
```

RabbitMQ health check 复用 `RabbitMqEventBusOptions`，默认名为 `rabbitmq`，并带有 `ready`、`messaging`、`rabbitmq` tags。

注册示例：

```csharp
services.AddLucidRabbitMqEventBus(
    configuration.GetRequiredSection(RabbitMqEventBusOptions.ConfigurationSectionName));

services.AddLucidRabbitMqHealthCheck();
```

注意事项：

- `AddLucidRabbitMqEventBus(...)` 注册 `RabbitMqEventBusOptions` 和 `IEventBus`。
- `AddLucidRabbitMqHealthCheck()` 依赖已有 `RabbitMqEventBusOptions`。
- 服务未实际接入 RabbitMQ 时，不应注册 RabbitMQ health check，避免 `/ready` 因外部依赖不可用而失败。
- 当前没有接入任何业务服务；业务服务需要发布集成事件时再按需注册。

## RabbitMQ Consumer 设计

RabbitMQ consumer 第一版采用代码注册和约定自动配置，不在配置文件中维护 consumer 列表。

注册 metadata 第一版使用方式：

```csharp
services.AddLucidRabbitMqConsumer<
    AdminUserCreatedIntegrationEvent,
    AdminUserCreatedIntegrationEventHandler>();
```

注册后框架根据事件类型和 handler 自动配置消费端：

- Event type：默认是 `AdminUserCreatedIntegrationEvent`，推荐通过 `IntegrationEventNameAttribute` 声明稳定事件名。
- Handler：`IIntegrationEventHandler<AdminUserCreatedIntegrationEvent>`。
- Binding key：使用事件名；优先使用 `IntegrationEventNameAttribute`，未声明时使用事件类型名。
- Queue name：默认使用 `{applicationName}.{handlerTypeName}`，即一个 consumer handler 一个队列。
- Exchange：复用 `RabbitMqEventBusOptions.ExchangeName`。

默认 queue name 中的 `applicationName` 使用 `IHostEnvironment.ApplicationName` 规范化生成，`handlerTypeName` 使用 handler 类型名规范化生成。

允许在代码注册时覆盖 queue name：

```csharp
services.AddLucidRabbitMqConsumer<
    AdminUserCreatedIntegrationEvent,
    AdminUserCreatedIntegrationEventHandler>(
    queueName: "identity.audit-events");
```

语义约定：

- 多个服务消费同一事件：每个服务注册自己的 queue。
- 同一服务多实例消费：多个实例使用同一个 queue，形成竞争消费。
- 同一服务多个 consumer：默认各自使用独立 queue。
- 同一服务多个 consumer 需要合并消费组：通过显式指定相同 queue name。

RabbitMQ consumer 拓扑：

- Declare topic exchange。
- Declare durable queue。
- Bind queue 到 exchange，binding key 默认为事件类型名。
- Consumer 使用 manual ack。
- Handler 成功返回后 ack。
- Handler 失败后 nack。
- 第一版默认 `RequeueOnFailure = false`，避免 poison message 无限重试。

RabbitMQ consumer 启动策略：

- 注册 RabbitMQ consumer 的服务，默认认为 RabbitMQ 是必要依赖。
- 应用启动时，consumer HostedService 会连接 RabbitMQ，并声明 exchange、queue 和 binding。
- 如果 RabbitMQ 连接或拓扑声明失败，consumer HostedService 会记录错误日志并让启动失败暴露出来。
- 第一版不做启动阶段无限重连，也不在后台静默等待 RabbitMQ 恢复，避免隐藏环境或配置问题。
- 后续如果需要服务在 RabbitMQ 暂不可用时仍可启动，再引入显式的 reconnect/startup retry 策略。

RabbitMQ consumer 失败处理策略：

- Handler 抛出异常时，consumer 会执行 `nack`。
- 默认 `RequeueOnFailure = false`，消息不会重新进入当前队列，避免持续阻塞同一个 consumer。
- 当前框架不会自动声明 DLX。
- 只有当队列由运维或后续框架能力配置了 DLX 时，`nack(requeue: false)` 的消息才会进入 dead-letter exchange。
- 如果队列没有配置 DLX，RabbitMQ 会丢弃该消息。
- 第一版不做自动 retry，也不在框架内声明 DLX。
- 需要短期重试时，业务服务可以临时显式设置 `requeueOnFailure: true`，但只建议用于确认不会产生 poison message 的场景。
- Consumer 启动时会记录 exchange、queue 和 binding keys。
- Handler 失败时会记录 event type、delivery tag 和是否 requeue。
- 如果处理过程因 consumer 停止信号取消，框架只记录取消日志，不再强行 ack/nack；未 ack 的消息会在 channel/connection 关闭后由 RabbitMQ 重新投递。

代码层保留 `RabbitMqConsumerFailureOptions` 作为后续扩展入口。当前只承载 `RequeueOnFailure`，后续可以在这里扩展 retry、dead-letter、告警或失败计数策略。

RabbitMQ consumer 与 Inbox 事务边界：

```text
收到 RabbitMQ message
  -> 反序列化 envelope
  -> 调用 IIntegrationEventHandler<TEvent>
  -> handler 内部可使用 IInboxMessageProcessor
  -> Inbox EF Core transaction commit
  -> handler 成功返回
  -> BasicAck
```

约定：

- RabbitMQ consumer 不在调用 handler 前 ack。
- 使用 Inbox 时，数据库 transaction 应在 handler 返回前完成 commit。
- 只有 handler 成功返回后，RabbitMQ consumer 才会 `BasicAck`。
- 如果 handler 抛异常，Inbox transaction 会回滚，RabbitMQ consumer 会 `BasicNack`。
- 如果 `BasicAck` 前进程崩溃，RabbitMQ 可能重新投递消息；Inbox 会按 integration event id 去重。
- EventBus 不内置 Inbox，也不强制所有 consumer 使用 Inbox；需要幂等保护的业务 handler 应显式组合 Inbox BuildingBlock。

类型映射：

- Envelope `Type` 第一版使用事件名。
- Consumer 根据 `Type` 找到已注册的 `IIntegrationEventHandler<TEvent>`。
- 版本治理策略后续再引入。

当前实现包含 consumer HostedService，会在应用启动时根据 registration metadata 自动声明 exchange、queue 和 binding，并启动 RabbitMQ consumer。

Consumer 第一版仍不实现 retry、dead-letter 或自动 Inbox middleware。

## IEventBus

应用层或基础设施层如需发布集成事件，应依赖 `LucidMicro.BuildingBlocks.EventBus.Abstractions` 中的 `IEventBus`。

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
```

约定：

- `integrationEvent` 必须非空。
- 发布语义由具体实现决定，抽象层不承诺事务、重试、顺序或至少一次投递。
- 需要事务一致性时，应在具体服务或后续 Outbox BuildingBlock 中处理。

## IIntegrationEventHandler

消费端处理器统一实现 `IIntegrationEventHandler<TEvent>`。

```csharp
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task HandleAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
```

约定：

- Handler 应保持幂等，后续接入消息中间件后可能出现重复投递。
- Handler 内部不应假设消息按顺序到达。
- Handler 失败后的重试、死信和告警由具体 EventBus 实现负责。

## 当前边界

当前暂不支持：

- retry / dead-letter 策略。
- Outbox / Inbox。
- baggage 和 RabbitMQ header 级 trace context 映射。
- 完整事件版本治理。
