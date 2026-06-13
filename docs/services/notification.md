# Notification 服务设计

本文档记录 Notification 服务的第一版轻量设计。

## 定位

Notification 服务负责统一对接通知渠道，例如短信、微信消息、邮件和站内信。

它不拥有其他服务的业务规则，只负责把“需要通知用户”的意图转换为具体渠道发送动作。

命名使用 `Notification`，避免和 MQ message、EventBus message 或业务消息模型混淆。

## 目标

第一版目标：

- 作为第一个真实消费 MQ 的业务服务。
- 验证 EventBus RabbitMQ consumer。
- 验证 Identity 通过 Outbox 发布集成事件。
- 沉淀跨服务通知的边界。

第一版不追求接入真实第三方平台。可以先使用 fake provider、日志 provider 或内存 provider 验证流程。

## 当前实现状态

当前已创建 Notification 服务骨架：

```text
backend/src/Services/Notification/
  LucidMicro.Services.Notification.Api/
  LucidMicro.Services.Notification.Application/
  LucidMicro.Services.Notification.Domain/
  LucidMicro.Services.Notification.Infrastructure/
```

已完成：

- Contracts 定义 `NotificationSendRequestedIntegrationEvent`，事件名为 `notification.send-requested.v1`。
- Identity 创建管理员用户时会写入通知请求 Outbox 消息。
- Domain 定义 `NotificationMessage`、`NotificationChannel`、`NotificationStatus`。
- Application 提供 `AddNotificationApplication` 注册入口。
- Application 提供 `NotificationApplicationService`，支持创建通知、按 id 查询和分页查看最近通知。
- Infrastructure 提供 `AddNotificationInfrastructure` 注册入口。
- Infrastructure 接入 `NotificationDbContext`、`notification_messages` 表和 PostgreSQL ready check。
- Infrastructure 接入 Inbox.EFCore 和 `inbox_messages` 表，作为 Notification consumer 的幂等记录。
- Application 接入 `IInboxMessageProcessor`，consumer handler 不再手写查重、标记和提交逻辑。
- Infrastructure 接入最小发送抽象：`INotificationSender`、`INotificationChannelSender` 和 `LogNotificationChannelSender`。
- Infrastructure 接入 RabbitMQ EventBus，并订阅 `NotificationSendRequestedIntegrationEvent`。
- Api 提供管理端查询入口 `GET /api/notifications/{id}`、`GET /api/notifications`，以及内部创建入口 `POST /internal/notifications`。经 Gateway 访问时，管理端查询入口为 `GET /api/notification/notifications/{id}` 和 `GET /api/notification/notifications`。
- 已生成 Notification 初始迁移、processed integration events 迁移、inbox messages 迁移和旧 processed 表删除迁移。
- Api 接入 Serilog、OpenTelemetry、ProblemDetails、全局异常处理和基础 Health Checks。

当前仍未接入：

- 真实短信、微信、邮件或站内信 provider。

## 服务边界

Notification 服务负责：

- 接收其他服务发布的集成事件。
- 根据事件生成通知任务。
- 选择通知渠道。
- 调用渠道 provider 发送通知。
- 记录通知发送状态。

Notification 服务不负责：

- 判断业务动作是否应该发生。
- 修改来源服务的业务数据。
- 暴露第三方平台细节给业务服务。
- 在第一版实现复杂模板、营销触达或用户偏好系统。

## 第一版事件流

推荐第一条链路：

```text
Identity
  -> outbox_messages
  -> Outbox publisher
  -> RabbitMQ topic exchange
  -> Notification consumer
  -> log notification provider
```

统一请求事件：

```text
notification.send-requested.v1
```

该事件由 Notification 契约项目定义：

```text
LucidMicro.Contracts.Notification
  IntegrationEvents/NotificationSendRequestedIntegrationEvent
```

事件表达“某个服务请求发送一条通知”，包含 `Recipient`、`Channel`、`Subject` 和 `Content`。

业务服务可以根据自己的规则决定是否发布通知请求事件。Notification 服务订阅该事件后，创建通知记录并调用对应渠道发送。

Identity 不应直接知道短信、微信或站内信实现，也不应引用 Notification 的 Domain、Application 或 Infrastructure。

需要同步调用 Notification 的场景，例如短信登录发码，应通过 Notification HTTP API 和 typed client 完成。HTTP 调用边界见 [服务间 HTTP 调用约定](../conventions/service-to-service-http.md)，不要让调用方引用 Notification.Application DTO。

## 第一版模型

第一版可以先使用很小的通知模型：

```text
NotificationMessage
  Id
  Recipient
  Channel
  Subject
  Content
  Status
  CreatedAt
  SentAt
  FailedAt
  FailureReason
```

`Channel` 候选值：

- `Sms`
- `WeChat`
- `Email`
- `InApp`

`Status` 候选值：

- `Pending`
- `Sent`
- `Failed`

第一版当前实现 `InApp` 和 `Sms` 的日志 sender，用来验证站内通知和短信登录流程，避免一开始引入真实第三方平台复杂度。

## 渠道抽象

Notification.Application 可以定义发送渠道抽象：

```csharp
public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }

    Task SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default);
}
```

Infrastructure 根据渠道选择具体实现。

第一版当前实现：

- `LogNotificationChannelSender`
- `LogSmsNotificationChannelSender`

后续再新增：

- `WeChatNotificationChannelSender`
- `EmailNotificationChannelSender`

## 与 BuildingBlocks 的关系

Notification.Api 已接入：

- `Logging.Serilog`：请求日志和 consumer 日志。
- `Observability.OpenTelemetry`：跨服务 trace。
- `OpenApi.AspNetCore`、`Cors.AspNetCore`、`HealthChecks.AspNetCore`：HTTP 宿主能力。

Notification.Infrastructure 已接入：

- `HealthChecks.Npgsql`：检查 Notification database ready 状态。
- `EventBus.RabbitMQ`：消费通知请求事件。
- `HealthChecks.RabbitMQ`：检查 RabbitMQ ready 状态。
- `Inbox.EFCore`：记录 consumer 幂等状态。

Identity.Infrastructure 已接入：

- `Outbox.Core`：启动 Outbox publisher。
- `EventBus.RabbitMQ`：提供 envelope publisher。
- `HealthChecks.RabbitMQ`：publisher 依赖 RabbitMQ 时检查 ready 状态。

## 幂等边界

Notification consumer 必须按 integration event id 做幂等处理。

原因：

- RabbitMQ 可能重复投递。
- Outbox 可能在“消息已发布但 published 状态未保存”时重复发布。
- Consumer 失败后可能重试。

当前第一版已接入 Inbox BuildingBlock，在 Notification 数据库中维护 `inbox_messages` 表，并通过 `IInboxMessageProcessor` 封装 consumer 幂等边界。

处理约定：

- Consumer 收到事件后先通过 `IInboxMessageProcessor` 按 `IntegrationEvent.Id` 判断是否已处理。
- 已处理过的事件直接返回，不再创建通知。
- 未处理过的事件会调用 `NotificationApplicationService.CreateAsync` 创建并发送通知。
- 创建成功后由 `IInboxMessageProcessor` 调用 `IInboxMessageStore` 记录 inbox message 并提交。

当前边界：

- Consumer 仍应把 handler 逻辑保持幂等，因为极端并发或“创建通知成功但记录 inbox message 失败”时仍可能出现重复投递。
- 旧的 `processed_integration_events` 表已通过后续迁移删除，当前 consumer 链路只依赖 `inbox_messages`。

### 当前事务边界

Notification consumer 当前通过 `IInboxMessageProcessor` 统一做查重、执行业务、标记 inbox 和提交。

Notification Infrastructure 通过 `AddLucidEfCoreInbox<NotificationDbContext>()` 注册 EF Core transaction，因此 MQ consumer 链路的数据库写入处于同一个 transaction：

```text
开启 EF Core transaction
  -> 创建 notification message
  -> NotificationApplicationService.CreateAsync 内部 SaveChanges
  -> 标记 inbox message
  -> Inbox processor SaveChanges
  -> commit transaction
```

因此 notification message 和 inbox message 的数据库提交边界已经收敛到同一个事务。

当前仍需注意：

- `LogNotificationChannelSender` 当前只是日志实现，适合第一版验证。
- 后续接入短信、微信等真实外部渠道时，发送动作不受数据库事务保护。
- 在真实渠道上线前，应重新设计发送动作和数据库状态之间的可靠性边界，例如发送任务表、重试或 provider 回执。

## 真实渠道可靠发送方案

真实短信、微信、邮件等 provider 不应直接放在 MQ consumer 的数据库事务里同步完成。数据库事务无法覆盖外部平台调用，如果直接发送，会遇到：

- 数据库提交成功，但 provider 调用失败。
- provider 调用成功，但数据库状态保存失败。
- provider 请求超时，但实际可能已经发送。
- RabbitMQ 重投或人工重试导致重复发送。

后续接入真实 provider 前，推荐把 Notification consumer 和真实发送动作拆开：

```text
RabbitMQ consumer
  -> Inbox 去重
  -> 创建 Pending notification_message
  -> commit
  -> Notification sender 后台任务扫描 Pending
  -> 调用真实 provider
  -> 标记 Sent / Failed
```

第一版演进方向：

- MQ consumer 只负责创建 `Pending` 通知记录，不直接调用真实外部 provider。
- 后台 sender 按批次领取 `Pending` 或可重试的 `Failed` 记录。
- 发送成功后标记 `Sent`，写入 `SentAt`。
- 发送失败后标记 `Failed`，写入 `FailedAt` 和 `FailureReason`。
- `FailureReason` 只保存简短、可展示的失败原因，完整异常堆栈保留在日志中，通过 traceId 关联。
- provider 超时按失败处理，交由后续 retry 策略判断是否重试。
- 为避免多实例重复领取，后台 sender 需要类似 Outbox 的 claim 字段，例如 `locked_until`。

后续可以再扩展：

- `failure_count`
- `next_retry_at`
- `provider_message_id`
- `provider_response`
- `idempotency_key`

边界约定：

- 真实 provider sender 必须具备幂等意识。
- 如果 provider 支持幂等键，优先使用 notification id 或派生 idempotency key。
- 如果 provider 不支持幂等键，应优先保证平台侧重复发送风险可接受，或引入更严格的业务防重策略。
- 当前 `LogNotificationChannelSender` 仍可保持同步发送，因为它没有真实外部副作用。

## 本地验证

本地手动验证步骤已拆到 [Notification 本地 E2E 验证](notification-e2e.md)。该文档覆盖数据库迁移、服务启动、Identity Outbox、RabbitMQ consumer、Notification Inbox 和常见排查。

## 推荐实施顺序

1. 创建 Notification 服务骨架。
2. 增加 Notification domain/application/infrastructure/api 项目。
3. 接入基础 BuildingBlocks：Persistence、Serilog、OpenTelemetry、HealthChecks。
4. 使用 `LucidMicro.Contracts.Notification` 中的 `NotificationSendRequestedIntegrationEvent`。
5. Identity 创建管理员用户时写入 Outbox。已完成。
6. Identity 启用 Outbox publisher 和 RabbitMQ envelope publisher。已完成。
7. Notification 注册 RabbitMQ consumer。已完成。
8. Notification consumer 使用 log provider 处理事件。已完成。
9. 再考虑真实短信、微信、站内信 provider。

## 当前不做

- 不接真实短信平台。
- 不接真实微信消息平台。
- 不设计完整模板系统。
- 不设计营销触达、订阅偏好、频控。
- 不设计通用消息中心 UI。
- 不用同步 HTTP 调用替代当前 Identity -> Outbox -> RabbitMQ -> Notification 的异步通知链路。
