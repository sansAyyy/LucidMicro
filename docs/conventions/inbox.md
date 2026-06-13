# Inbox 约定

本文档记录当前后端 Inbox 的最小约定。

## 目标

Inbox 用于解决消费者重复处理集成事件的问题。

典型来源：

- RabbitMQ 可能重复投递。
- Outbox 可能在“消息已发布但 published 状态未保存”时重复发布。
- Consumer 处理成功后，确认消息前服务崩溃。

Inbox 的目标不是替代 handler 的业务幂等，而是提供一层按 integration event id 去重的基础设施。

## 项目结构

当前 Inbox BuildingBlock 包含：

```text
BuildingBlocks/Messaging/Inbox/
  LucidMicro.BuildingBlocks.Inbox.Abstractions/
  LucidMicro.BuildingBlocks.Inbox.Core/
  LucidMicro.BuildingBlocks.Inbox.EFCore/
```

当前提供抽象、Core 处理器和 EF Core 最小持久化实现。Notification 服务已接入 Inbox EF Core store，使用 `inbox_messages` 记录已处理的 integration event。

## InboxMessage

`InboxMessage` 是消费者处理记录的最小模型。

```csharp
public sealed record InboxMessage
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

字段约定：

- `Id` 使用 integration event id。
- `Type` 使用 EventBus 事件名，即 `IntegrationEventNameResolver` 解析后的名称。
- `ProcessedAt` 是消费者完成处理的时间。
- `CreatedAt` 是记录写入 inbox 的时间。

## IInboxMessageStore

`IInboxMessageStore` 描述消费者幂等记录的最小持久化能力。

```csharp
public interface IInboxMessageStore
{
    Task<bool> HasProcessedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
```

约定：

- Consumer handler 开始处理前，先按 `IntegrationEvent.Id` 调用 `HasProcessedAsync`。
- 已处理过的事件直接返回。
- 处理成功后调用 `MarkProcessedAsync`。
- 处理失败时不要标记 processed，让消息按 consumer 失败策略处理。
- `MarkProcessedAsync` 的具体实现负责解析事件名并写入处理记录。
- `SaveChangesAsync` 提交 inbox store 当前挂起的变更。
- Consumer 可以在业务处理和 inbox 标记之间选择同一个事务边界；第一版不强制抽象层自动提交。
- 第一版不在抽象中定义重试、锁、清理、TTL 或 poison message 策略。

## 与 Outbox 的关系

Outbox 负责发布端可靠投递，Inbox 负责消费端幂等处理。

推荐链路：

```text
业务数据 + outbox message
  -> Outbox publisher
  -> MQ
  -> Consumer
  -> Inbox 去重
  -> Handler 处理业务
```

即使发布端使用 Outbox，消费端仍然需要 Inbox 或业务幂等保护，因为消息可能重复到达。

## IInboxMessageProcessor

`IInboxMessageProcessor` 封装 EventBus consumer 常见的幂等处理流程：

```csharp
public interface IInboxMessageProcessor
{
    Task ProcessAsync<TEvent>(
        TEvent integrationEvent,
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
```

处理顺序：

- 先通过 `IInboxMessageStore.HasProcessedAsync` 判断事件是否处理过。
- 已处理过的事件直接返回，不调用业务 handler。
- 未处理过的事件先执行业务 handler。
- 业务 handler 成功后调用 `MarkProcessedAsync` 和 `SaveChangesAsync`。
- 业务 handler 抛异常时不标记 inbox，让消息按 consumer 失败策略处理。

Consumer handler 推荐只保留业务处理逻辑：

```csharp
public Task HandleAsync(
    NotificationSendRequestedIntegrationEvent integrationEvent,
    CancellationToken cancellationToken = default)
{
    return inboxProcessor.ProcessAsync(integrationEvent, ProcessAsync, cancellationToken);
}
```

注册示例：

```csharp
services.AddLucidInboxProcessor();
```

## 事务边界

Inbox consumer 的理想提交边界是：

```text
HasProcessed
  -> 执行业务变更
  -> MarkProcessed
  -> SaveChanges
```

如果注册了 EF Core Inbox，processor 会通过 EF Core transaction 包裹业务 handler、inbox 标记和最终保存。即使业务 handler 内部调用了 `SaveChangesAsync`，这些数据库写入也仍处在同一个 transaction 中，直到 processor 提交事务。

约定：

- `Inbox.Core` 只定义事务边界抽象，默认实现为 no-op。
- `Inbox.EFCore` 注册 `IInboxProcessingTransaction`，基于当前 `TDbContext.Database.BeginTransactionAsync()` 开启事务。
- 如果当前 DbContext 已经存在 transaction，EF Core 实现会复用当前事务上下文，不再额外开启嵌套事务。
- 数据库事务只覆盖同一个 DbContext/连接上的数据库写入。
- 如果业务 handler 写入其他 DbContext、Redis、HTTP、MQ 或第三方平台，这些动作不受该数据库事务保护。
- Consumer 业务逻辑仍应尽量保持幂等，尤其是外部副作用和不可重复创建的数据。

推荐：

- 新 consumer 优先复用服务自己的 DbContext 和 `AddLucidEfCoreInbox<TDbContext>()` 注册。
- 面向 HTTP/API 的应用服务可以继续自提交；在 Inbox processor 的 EF Core transaction 内调用时，`SaveChangesAsync` 会 flush 到当前 transaction，最终由 processor commit。
- 接入真实外部渠道前，应单独设计外部副作用的可靠性边界，避免“外部已发送但数据库事务回滚”的问题。

## EF Core 持久化

Inbox EF Core 第一版提供：

- `InboxMessageEntity`
- `InboxModelBuilderExtensions.ConfigureInbox()`
- `EfCoreInboxMessageStore<TDbContext>`
- `EfCoreInboxProcessingTransaction<TDbContext>`
- `AddLucidEfCoreInbox<TDbContext>()`

固定表名：

```text
inbox_messages
```

推荐字段：

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | yes | integration event id |
| `type` | `varchar(256)` | yes | EventBus 事件名 |
| `processed_at` | `timestamptz` | yes | 处理完成时间 |
| `created_at` | `timestamptz` | yes | 记录写入时间 |

DbContext 配置示例：

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureInbox();
}
```

注册示例：

```csharp
services.AddLucidEfCoreInbox<NotificationDbContext>();
```

提交约定：

- `MarkProcessedAsync` 只把 inbox message 加入当前 DbContext。
- `SaveChangesAsync` 提交 store 当前挂起的变更。
- 如果 consumer 需要把业务处理和 inbox 标记放在同一事务内，应使用同一个 DbContext/事务边界。
- `AddLucidEfCoreInbox<TDbContext>()` 会注册 EF Core transaction 实现，让 `IInboxMessageProcessor` 使用同一个 `TDbContext` 开启事务。
- 第一版不自动吞掉重复主键异常；重复标记由数据库约束兜底，调用方按 consumer 失败策略处理。
- 第一版不支持自定义表名，统一使用 `inbox_messages`，保持和 Outbox 固定 `outbox_messages` 的约定一致。

## 当前边界

当前仍不做：

- 通用 consumer middleware。
- Inbox 后台清理任务。
- DLQ 或 poison message 管理。
