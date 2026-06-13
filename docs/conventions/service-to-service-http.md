# 服务间 HTTP 调用约定

本文档定义业务服务之间通过 HTTP 同步调用时的最小约定。

第一条样板链路是 Identity 调 Notification，用于验证 `ServiceDiscovery.Http`、`ServiceDiscovery.Consul` 和 `Resilience.Http` 的组合方式。

## 适用场景

推荐使用服务间 HTTP：

- 调用方需要同步拿到结果。
- 下游调用属于当前请求的一部分，例如短信登录发码。
- 调用失败需要立即反馈给用户或调用方。
- 下游服务已经暴露稳定 HTTP API。

不推荐使用服务间 HTTP：

- 只表达“某件事发生了”，不需要同步结果。
- 调用方不应该被下游可用性阻塞。
- 适合异步最终一致，例如创建管理员用户后发送通知。

异步通知、跨服务事件和最终一致仍优先使用 EventBus + Outbox + Inbox。

## Identity 调 Notification

短信登录发码推荐走同步 HTTP：

```text
Identity.Api
  -> INotificationClient
  -> typed HttpClient
  -> ServiceDiscovery.Http
  -> Resilience.Http
  -> Notification.Api
```

创建管理员用户后的通知仍推荐走 MQ：

```text
Identity
  -> Outbox
  -> RabbitMQ
  -> Notification consumer
```

判断标准很简单：用户当前请求是否必须立即知道发送请求是否被 Notification 接收。

## 契约边界

调用方不引用被调用服务的 Application、Domain 或 Infrastructure 项目。

Notification 对外 HTTP 契约应由 Notification 服务拥有，并放在公共 Contracts 项目中，例如：

```text
backend/src/Contracts/LucidMicro.Contracts.Notification/
  Http/
    Requests/
    Responses/
```

第一版可以先保持很薄：

```csharp
public sealed record SendNotificationRequest(
    string? Recipient,
    string Channel,
    string? Subject,
    string? Content);
```

HTTP contract 中的 `Channel`、`Status` 等枚举语义对外使用稳定字符串，例如 `InApp`、`Sent`，不要暴露服务内部 enum 数值。

后续如果 OpenAPI client 生成流程成熟，可以让 `INotificationClient` 使用生成的 DTO，但业务服务仍不直接引用 Notification.Application DTO。

## Client 放置

调用方的 Application 层只依赖一个端口，例如：

```csharp
public interface INotificationClient
{
    Task SendAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken = default);
}
```

HTTP 实现放在调用方 Infrastructure：

```text
LucidMicro.Services.Identity.Infrastructure/
  ExternalServices/
    Notifications/
      NotificationClient.cs
```

这样 Identity.Application 不知道 HTTP、服务发现、Resilience 或具体地址来源。

当前 Identity 已落地：

```text
LucidMicro.Services.Identity.Application/
  ExternalServices/Notifications/INotificationClient.cs

LucidMicro.Services.Identity.Infrastructure/
  ExternalServices/Notifications/NotificationClient.cs
```

该 client 先作为同步调用基础设施存在，暂不替换现有管理员创建后的 MQ 通知链路。

## 注册约定

业务服务的 Infrastructure 层负责组合 provider 和 typed client，服务启动层优先调用服务级注册入口，例如：

```csharp
builder.Services.AddIdentityInfrastructure(builder.Configuration);
```

这样 `Program.cs` 不需要暴露服务内部依赖了哪些下游 client、缓存或服务发现 provider。

BuildingBlock 级注册仍保持独立，供服务 Infrastructure 组合使用。服务间 HTTP 默认使用 Consul provider：

```csharp
builder.Services.AddLucidConsulServiceDiscovery(
    builder.Configuration.GetRequiredSection(
        LucidConsulServiceDiscoveryOptions.ConfigurationSectionName));
builder.Services.AddLucidConsulHealthCheck();
```

Notification typed client 在 Identity.Infrastructure 内部注册为：

```csharp
services
    .AddHttpClient<INotificationClient, NotificationClient>()
    .AddLucidServiceDiscovery("notification")
    .AddLucidStandardHttpResilienceHandler(...);
```

该注册入口内部使用 typed `HttpClient`，并接入 `AddLucidServiceDiscovery("notification")` 和 `AddLucidStandardHttpResilienceHandler(...)`。

约定：

- 服务名使用小写短名，例如 `notification`。
- typed client 内只使用相对路径，例如 `internal/notifications`。
- 不在业务代码里拼接 host、端口或环境地址。
- Resilience 显式接到需要保护的 HTTP client 上，不默认覆盖所有 client。

## 配置示例

```json
{
  "Lucid": {
    "ServiceDiscovery": {
      "Consul": {
        "Address": "http://localhost:8500",
        "Datacenter": "",
        "Token": "",
        "OnlyPassing": true,
        "CacheDurationSeconds": 10,
        "RequestTimeoutSeconds": 5
      }
    }
  }
}
```

本地开发也使用 Consul。调用前需要确保 Consul 中已经存在 `notification` 服务实例，并且该实例处于 passing 状态。
当前 Notification 启动时会通过 Consul service registration 自动注册 `notification` 实例。

## 错误边界

`INotificationClient` 应把 HTTP 传输异常、超时和非成功状态转换为调用方能理解的应用错误。

第一版建议：

- `2xx`：视为下游已接收请求。
- `4xx`：通常不重试，映射为调用方可处理的错误。
- `5xx`、超时、连接失败：交给 `Resilience.Http` 处理，最终失败后映射为下游不可用。

如果下游返回标准 ProblemDetails，可以读取 `code` 和 `title` 用于诊断，但调用方仍应包成自己的应用错误，不把下游 Application 错误类型泄漏到业务用例中。

通用的发送与错误收口逻辑放在 `LucidMicro.BuildingBlocks.Http.Core`：

```csharp
return await httpClient.PostAsJsonForResultAsync(
    "internal/notifications",
    request,
    requestFailedCode: "Identity.Notification.RequestFailed",
    unavailableCode: "Identity.Notification.Unavailable",
    serviceName: "Notification service",
    timeoutMessage: "Notification service request timed out.",
    unavailableMessage: "Notification service is unavailable.",
    cancellationToken);
```

其中 `requestFailedCode` 表示下游已返回 HTTP 响应但不是成功状态，`unavailableCode` 表示连接失败、超时或最终不可达。调用方仍然拥有自己的错误码命名空间。

不要在业务服务里解析下游内部异常文本，也不要让业务规则依赖下游错误消息。

## 与 MQ 的关系

HTTP 和 MQ 不是替代关系。

推荐边界：

- 强交互、当前请求要立即知道结果：HTTP。
- 异步通知、最终一致、副作用可延迟：MQ + Outbox + Inbox。

因此短信登录可以走 HTTP，管理员创建后的欢迎通知仍可以走 MQ。

## 当前不做

- 不做跨服务通用 SDK 生成器。
- 不把所有服务 HTTP client 放进一个大 Contracts 包。
- 不让业务服务引用其他服务的 Application DTO。
- 不在 ServiceDiscovery 里处理业务错误、鉴权或重试。
