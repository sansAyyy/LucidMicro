# Outbox 约定

本文档记录当前后端 Outbox 的最小约定。

## 目标

Outbox 用于解决业务数据写入和集成事件发布之间的一致性问题。

典型问题：

- 业务数据库写入成功，但发布 MQ 失败。
- 发布 MQ 成功，但业务事务回滚。
- 服务重启或网络抖动导致事件丢失。

Outbox 的目标不是让 MQ 发布参与数据库事务，而是把“要发布的消息”先作为业务事务的一部分写入数据库，再由后台发布器异步投递。

## 项目结构

当前 Outbox BuildingBlock 包含：

```text
BuildingBlocks/Messaging/Outbox/
  LucidMicro.BuildingBlocks.Outbox.Abstractions/
  LucidMicro.BuildingBlocks.Outbox.Core/
  LucidMicro.BuildingBlocks.Outbox.EFCore/
```

当前定义抽象、最小 System.Text.Json 序列化实现、默认 publisher、后台调度器和 EF Core 持久化 store。

Identity 服务已接入 Outbox EF Core 基础设施：`IdentityDbContext` 配置 `outbox_messages` 表，Infrastructure 注册 `IOutboxEventWriter`、`IOutboxMessageStore` 和 `IOutboxMessageSerializer`。Identity 创建管理员用户时会写入 `notification.send-requested.v1` outbox message，Identity.Api 已启用 Outbox publisher 和 RabbitMQ envelope publisher。

## 基本流程

推荐流程：

1. 应用服务处理命令。
2. 在同一个数据库事务内写业务数据。
3. 在同一个数据库事务内写 outbox message。
4. 事务提交。
5. 后台 publisher claim pending outbox message。
6. publisher 将 outbox message 转成 `IntegrationEventEnvelope` 并发布。
7. 发布成功后标记 outbox message 为 published。
8. 发布失败后记录失败信息，等待后续重试策略处理。

约定：

- 应用层不应在业务事务中直接调用 RabbitMQ。
- Outbox message 必须和业务数据落在同一个持久化边界内。
- 发布端应具备幂等意识，因为后台 publisher 可能重复发布。
- 消费端仍然需要 Inbox 或业务幂等保护，Outbox 不解决消费端重复处理问题。
- Outbox publisher 不把 `Type + Payload` 反序列化回 CLR integration event。
- Outbox publisher 发布 envelope，避免依赖事件类型注册表或反射调用泛型 `IEventBus`。

## OutboxMessage

`OutboxMessage` 是持久化待发布消息的最小模型。

```csharp
public sealed record OutboxMessage
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public string? TraceParent { get; init; }

    public string? TraceState { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PublishedAt { get; init; }

    public int FailureCount { get; init; }

    public string? LastError { get; init; }
}
```

字段约定：

- `Id` 是 outbox message 标识，通常和 integration event id 保持一致。
- `Type` 使用 EventBus 事件名，即 `IntegrationEventNameResolver` 解析后的名称。
- `OccurredAt` 是事件发生时间。
- `TraceParent` / `TraceState` 用于延续 trace context。
- `Payload` 是事件内容序列化结果。
- `CreatedAt` 是写入 outbox 的时间。
- `PublishedAt` 有值表示已经成功发布。
- `FailureCount` 和 `LastError` 为后续重试、告警或排障预留。

## IOutboxEventWriter

`IOutboxEventWriter` 是应用服务写入 outbox 的推荐入口。

```csharp
public interface IOutboxEventWriter
{
    Task AddAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
```

约定：

- 应用服务优先依赖 `IOutboxEventWriter`，不直接组合 serializer 和 store。
- 默认实现会使用 `IOutboxMessageSerializer` 把 integration event 转成 `OutboxMessage`。
- 默认实现随后调用 `IOutboxMessageStore.AddAsync` 写入当前持久化边界。
- `AddAsync` 不会自动调用 `SaveChanges`，应由业务事务或 Unit of Work 统一提交。

## 事务边界

Outbox 写入端的理想提交边界是：

```text
写业务数据
  -> 写 outbox message
  -> SaveChanges
```

当前 `IOutboxEventWriter.AddAsync` 只写入待提交的 outbox message，不调用 `SaveChanges`。因此应用服务可以像 Identity 创建管理员用户那样，在同一个 Unit of Work 中同时提交业务数据和 outbox message。

约定：

- 业务写入端：由业务 Unit of Work 提交，Outbox writer 不自带提交。
- 后台发布端：由 Outbox publisher 调用 `IOutboxMessageStore.SaveChangesAsync` 提交 published/failed 状态。
- 这两个路径共用 `IOutboxMessageStore`，但提交责任不同。
- 不建议在业务写入端直接调用 `IOutboxMessageStore.SaveChangesAsync`，否则会破坏业务数据和 outbox message 的原子提交边界。

## IOutboxMessageStore

`IOutboxMessageStore` 描述 outbox message 的持久化能力。

```csharp
public interface IOutboxMessageStore
{
    Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset? nextRetryAt,
        DateTimeOffset? deadAt,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
```

约定：

- `AddAsync` 应在业务事务内调用。
- `ClaimPendingAsync` 领取一批可发布消息，实现可以在该方法内写入 claim 状态。
- `MarkAsPublishedAsync` 在发布成功后调用。
- `MarkAsFailedAsync` 在发布失败后调用，`nextRetryAt` 表示下次可重试时间，`deadAt` 表示已达到最大重试次数并终止自动发布。
- `SaveChangesAsync` 提交 store 当前挂起的变更。
- 业务事务内写入 outbox 时，可以继续由业务 Unit of Work 统一提交。
- Outbox publisher 标记 published/failed 后，应调用 store 自带的 `SaveChangesAsync` 提交状态变更。

## IOutboxPublisher

`IOutboxPublisher` 描述后台发布能力。

```csharp
public interface IOutboxPublisher
{
    Task PublishPendingAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default);
}
```

约定：

- publisher 从 `IOutboxMessageStore` claim pending message。
- publisher 将 `OutboxMessage` 映射为 `IntegrationEventEnvelope` 后发布。
- 发布成功后标记 published。
- 发布失败后根据 retry options 标记 failed，并写入下一次重试时间或 dead 时间。
- 标记状态后，publisher 通过 `IOutboxMessageStore.SaveChangesAsync` 提交 outbox 状态变更。
- 当前抽象不规定调度方式，可以由 HostedService、定时任务或手动触发实现。

Publisher 发布方向：

- 业务代码仍然通过 `IEventBus.PublishAsync<TEvent>(...)` 发布即时消息。
- Outbox publisher 不依赖泛型 `IEventBus`。
- 具体 EventBus provider 应提供 envelope 发布能力，例如 `IIntegrationEventEnvelopePublisher`。
- Outbox publisher 直接发布 envelope，不需要知道 `Type` 对应哪个 CLR 类型。
- 这样可以避免把 outbox message 和 .NET 类型名强绑定，也避免引入事件类型注册表。

当前默认实现为 `DefaultOutboxPublisher`。

处理约定：

- 每次调用 claim 最多 `maxCount` 条 pending message。
- 每条消息独立发布和提交状态。
- 发布成功后标记 `PublishedAt`。
- 发布失败后记录 `LastError`、递增失败次数，并按指数退避写入 `NextRetryAt`。
- 失败次数达到 `MaxRetryCount` 后写入 `DeadAt`，该消息不再被自动 claim。
- 每条消息处理完成后调用一次 `IOutboxMessageStore.SaveChangesAsync`。
- 第一版优先保证状态及时落库，不做批量提交优化。
- 发布成功时记录 message id 和 type。
- 未达到最大重试次数时，发布失败记录 warning，并包含 message id、type、failure count 和 next retry time。
- 达到最大重试次数时，发布失败记录 error，并包含 message id、type、failure count 和 dead time。
- `OperationCanceledException` 不会被吞掉，会继续向调度层抛出。
- 如果标记 failed 或保存状态本身失败，异常会继续冒出，由调度层处理。

注册示例：

```csharp
services.AddLucidOutboxPublisher();
```

## 后台调度器

Outbox.Core 提供最小后台调度器 `OutboxPublisherHostedService`。

注册示例：

```csharp
services.AddLucidOutboxPublisherHostedService(options =>
{
    options.Interval = TimeSpan.FromSeconds(10);
    options.BatchSize = 50;
    options.MaxRetryCount = 10;
    options.InitialRetryDelay = TimeSpan.FromSeconds(30);
    options.MaxRetryDelay = TimeSpan.FromMinutes(30);
    options.RetryBackoffFactor = 2;
});
```

需要发布到 RabbitMQ 的服务还应注册 RabbitMQ EventBus provider：

```csharp
services.AddLucidRabbitMqEventBus(
    configuration.GetRequiredSection(RabbitMqEventBusOptions.ConfigurationSectionName));
```

调度约定：

- HostedService 启动后立即执行一轮发布。
- 每轮通过 scoped `IOutboxPublisher` 调用 `PublishPendingAsync`。
- 每轮最多处理 `BatchSize` 条消息。
- 两轮之间等待 `Interval`。
- 单轮执行失败只记录日志，不让 HostedService 直接退出。
- `OperationCanceledException` 作为停止信号处理。
- Publisher 发布失败时使用指数退避控制下一次重试时间。
- 当前不做分布式锁。
- 多实例部署应使用 PostgreSQL store 的 claim 保护，其他 provider 需要单独评估。

## IOutboxMessageSerializer

`IOutboxMessageSerializer` 描述把 integration event 转成 outbox message 的能力。

```csharp
public interface IOutboxMessageSerializer
{
    OutboxMessage Serialize<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent;
}
```

当前默认实现为 `SystemTextJsonOutboxMessageSerializer`。

序列化约定：

- `Id` 使用 `IntegrationEvent.Id`。
- `Type` 使用 `IntegrationEventNameResolver` 解析后的事件名。
- `OccurredAt` 使用 `IntegrationEvent.OccurredAt`。
- `TraceParent` / `TraceState` 从 `Activity.Current` 读取。
- `Payload` 使用 `System.Text.Json` 按 web defaults 序列化。

当前不拆独立 serialization 类库。后续如果出现多个序列化实现，再考虑拆分。

## EF Core / PostgreSQL 持久化

Outbox 持久化第一版基于 EF Core，面向 PostgreSQL 表结构约定。

生产目标 provider 是 PostgreSQL。SQLite 只用于轻量单元测试，不能代表 PostgreSQL 的全部行为，尤其是 `jsonb`、partial index 和 `DateTimeOffset` 查询翻译等 provider-specific 行为。

当前结构：

```text
BuildingBlocks/Messaging/Outbox/
  LucidMicro.BuildingBlocks.Outbox.EFCore/
```

每个服务维护自己的 outbox table。Outbox table 应与业务数据位于同一个数据库和同一个事务边界内，不使用独立 outbox 数据库。

推荐表名：

```text
outbox_messages
```

推荐字段：

| Column | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | yes | outbox message id，通常等于 integration event id |
| `type` | `varchar(256)` | yes | 事件名，例如 `identity.admin-user.created.v1` |
| `occurred_at` | `timestamptz` | yes | 事件发生时间 |
| `trace_parent` | `varchar(128)` | no | W3C traceparent |
| `trace_state` | `varchar(512)` | no | W3C tracestate |
| `payload` | `jsonb` | yes | 事件 payload |
| `created_at` | `timestamptz` | yes | 写入 outbox 的时间 |
| `published_at` | `timestamptz` | no | 成功发布的时间 |
| `locked_until` | `timestamptz` | no | publisher claim 到期时间 |
| `next_retry_at` | `timestamptz` | no | 发布失败后的下一次可重试时间 |
| `dead_at` | `timestamptz` | no | 达到最大重试次数后的终止时间 |
| `failure_count` | `integer` | yes | 发布失败次数，默认 0 |
| `last_error` | `text` | no | 最近一次发布失败错误 |

推荐索引：

```sql
create index ix_outbox_messages_pending
    on outbox_messages (created_at)
    where published_at is null and dead_at is null;

create index ix_outbox_messages_published_at
    on outbox_messages (published_at);
```

EF Core 集成约定：

- 每个服务自己的 DbContext 持有自己的 `outbox_messages`。
- `IOutboxMessageStore.AddAsync` 必须复用业务 DbContext。
- 应用服务在同一个事务内写业务数据和 outbox message。
- `SaveChanges` 或 Unit of Work 提交时，业务数据和 outbox message 一起提交。
- Outbox EF Core 实现不应自己创建独立 DbContext 事务包裹业务写入。
- 业务 DbContext 在 `OnModelCreating` 中调用 `modelBuilder.ConfigureOutbox()`。
- `ClaimPendingAsync` 只领取未发布、未 dead、未锁定或锁已过期、且 `next_retry_at` 为空或已到期的消息。

注册示例：

```csharp
services.AddLucidEfCoreOutbox<IdentityDbContext>();
```

写入示例：

```csharp
await outbox.AddAsync(integrationEvent, cancellationToken);
```

`AddAsync` 不会自动调用 `SaveChanges`，应由业务事务或 Unit of Work 统一提交。

Publisher 提交约定：

- Outbox publisher 不依赖业务 `IUnitOfWork`。
- Outbox publisher 使用 `IOutboxMessageStore.SaveChangesAsync` 提交 published/failed 状态。
- EF Core store 的 `SaveChangesAsync` 直接调用当前业务 DbContext 的 `SaveChangesAsync`。
- 业务写入路径和 publisher 路径共享同一个 store，但提交边界不同：业务写入由业务事务提交，publisher 状态更新由 store 自己提交。

Publisher 查询约定：

- 第一版按 `created_at` 升序 claim pending message。
- pending message 定义为 `published_at is null and dead_at is null`。
- 可 claim message 定义为未发布、未 dead、未锁定或锁已过期，并且 `next_retry_at` 为空或已到期。
- 查询时限制 `maxCount`，避免一次加载过多消息。
- PostgreSQL 使用 `for update skip locked` 原子 claim 一批消息，并设置 `locked_until`。
- claim 成功后，如果发布成功会清除 lock 并写入 `published_at`。
- claim 成功后，如果发布失败会清除 lock、递增 `failure_count`、写入 `last_error`，并设置 `next_retry_at` 或 `dead_at`。
- 如果 publisher 实例崩溃，消息会在 `locked_until` 过期后重新变成可 claim。
- SQLite 测试环境下，`DateTimeOffset` 排序和 claim 使用轻量兜底；SQLite 不代表生产多实例并发能力。
- 第一版不承诺严格顺序，因为失败重试、多实例和事务提交时间都可能影响顺序。

多实例并发边界：

- PostgreSQL store 支持基础多实例读取保护。
- 多个 publisher 实例可以同时运行，每个实例 claim 自己的一批消息。
- 当前不使用 Redis 分布式锁，不把整个 publisher 串行化。
- 当前支持 publisher 侧指数退避和 dead 标记。
- 消费端仍然必须保证幂等，因为消息可能在“发布成功但标记 published 前失败”的场景下重复投递。

PostgreSQL 集成测试：

- `OutboxPostgreSqlIntegrationTests` 默认跳过。
- 设置 `LUCID_TEST_POSTGRESQL_CONNECTION_STRING` 后才会运行。
- 连接串必须指向测试专用 PostgreSQL database。
- 测试会创建并删除 `lucid_outbox_tests` schema。
- 当前核心验证点是两个 publisher 同时 claim 时拿到不同 message，并真实执行 PostgreSQL claim SQL。

## 当前边界

当前暂不支持：

- RabbitMQ dead-letter exchange 自动声明和投递。
- dead outbox message 的后台告警、人工重放或管理 API。
- 在 Outbox 内部处理消费端幂等；消费端应使用 Inbox 或业务幂等保护。
