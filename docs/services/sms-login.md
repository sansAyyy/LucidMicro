# 短信登录技术设计

本文档记录短信登录的第一版技术方案。它既是一个用户功能，也是一个框架能力验证场景，用来串起 Redis、HTTP Resilience、Notification 和服务发现能力。

## 目标

第一版目标：

- Identity 负责发起短信验证码流程。
- Notification 负责统一发短信。
- Redis 负责验证码、限频和短期状态。
- HTTP 调用接入 `Resilience.Http`。
- 通过服务发现解析 Notification 地址。

第一版不追求真实短信平台。可以先用 Notification 的日志渠道或站内模拟渠道完成闭环。

## 当前实现状态

当前已在 Identity.Application 落地短信登录应用层骨架：

```text
Features/SmsLogin/
  Abstractions/ISmsLoginApplicationService.cs
  Dtos/Requests/SendSmsLoginCodeRequest.cs
  Dtos/Requests/LoginBySmsCodeRequest.cs
  Dtos/Responses/SmsLoginResponse.cs
  Errors/SmsLoginErrors.cs
  Services/SmsLoginApplicationService.cs
  Validators/
```

已完成：

- 定义 `SendCodeAsync` 和 `LoginAsync` 应用层端口。
- 定义发送验证码、短信登录和登录响应 DTO。
- 接入请求验证器。
- `AddIdentityApplication()` 默认注册短信登录应用服务。
- 定义 `ISmsLoginCodeStore` 验证码存储端口。
- Infrastructure 提供 `RedisSmsLoginCodeStore`，基于 `ICacheService` 管理验证码、限频和尝试次数。
- `AddIdentityInfrastructure(...)` 默认组合短信登录所需的 Redis、Service Discovery、Resilience 和 Notification HTTP client。
- `SendCodeAsync` 已串起限频判断、验证码生成、验证码保存和 Notification 同步发送。
- `LoginAsync` 已串起验证码读取、错误尝试计数、达到上限清理验证码和成功匹配后一次性失效。
- `LoginAsync` 已按 `AdminUser.PhoneNumber` 查找管理员，并复用管理员 token 签发逻辑返回登录结果。
- 管理员手机号已加软删除过滤唯一约束，避免同一手机号对应多个管理员。
- Identity.Api 已提供短信登录最小 HTTP 入口。

当前仍未接入：

- 真实短信 provider。

## 流程

推荐链路：

```text
Client
  -> Identity.Api 请求短信验证码
  -> Redis 写入验证码和限频状态
  -> Identity.Api 调用 Notification.Api
  -> Resilience.Http 处理超时 / 重试 / 熔断
  -> Service Discovery 负责定位 Notification.Api
  -> Notification.Api 记录通知并调用渠道 sender
```

服务间 HTTP 调用的通用约定见 [服务间 HTTP 调用约定](../conventions/service-to-service-http.md)。

当前 `SendCodeAsync` 链路：

```text
校验手机号
  -> ISmsLoginCodeStore.CanSendAsync
  -> ISmsLoginCodeGenerator.Generate
  -> ISmsLoginCodeStore.SaveCodeAsync
  -> INotificationClient.SendAsync
```

如果 Notification 发送失败，会删除刚保存的验证码，避免用户收到失败响应但验证码仍可用。

当前 `LoginAsync` 链路：

```text
校验手机号和验证码
  -> ISmsLoginCodeStore.GetCodeAsync
  -> 验证码不存在时返回 CodeExpired
  -> 验证码不匹配时 IncrementAttemptAsync
  -> 达到 MaxAttempts 后 RemoveCodeAsync
  -> 验证码匹配后 RemoveCodeAsync
  -> 按 AdminUser.PhoneNumber 查找管理员
  -> 校验管理员是否启用
  -> 更新最近登录时间
  -> 签发 access token 和 refresh token
```

短信登录使用现有管理员身份模型，登录目标是 `AdminUser`。

短信登录功能在 Identity.Api 中作为正式能力注册。启动层只需要保持服务级注册：

```csharp
services.AddIdentityApplication();
services.AddIdentityInfrastructure(configuration);
```

`AddIdentityApplication()` 注册应用层服务，`AddIdentityInfrastructure(...)` 负责组合该服务运行需要的基础设施。短信登录不再拆独立注册入口，避免 API 启动层暴露太多功能内部细节。

本地默认配置：

- Redis：`localhost:6379`
- Consul：`http://localhost:8500`
- Notification 服务名：`notification`
- 默认管理员手机号：`13800138000`

## 职责划分

### Identity

Identity 负责：

- 校验手机号格式。
- 生成验证码。
- 将验证码写入 Redis。
- 控制发送频率。
- 调用 Notification 发短信。
- 处理短信登录时的验证逻辑。

Identity 不负责：

- 直接对接第三方短信平台。
- 关心 Notification 内部的消息模型。
- 关心具体 provider 的重试细节。

### Notification

Notification 负责：

- 接收 Identity 的短信发送请求。
- 记录通知任务。
- 选择渠道。
- 调用短信 provider。
- 记录发送结果和失败原因。

Notification 不负责：

- 生成登录验证码。
- 维护登录限频规则。
- 决定某个手机号是否允许发短信。

## Redis 约定

建议使用以下 key：

```text
identity:sms-login:code:{phone}
identity:sms-login:rate:{phone}
identity:sms-login:attempt:{phone}
```

建议含义：

- `code`：短信验证码，TTL 例如 5 分钟。
- `rate`：发码限频，TTL 例如 60 秒。
- `attempt`：验证码校验次数或失败次数，TTL 可与验证码一致。

当前 Redis store 已使用以上 key 约定：

```text
identity:sms-login:code:{phone}
identity:sms-login:rate:{phone}
identity:sms-login:attempt:{phone}
```

当前默认配置：

```json
{
  "Lucid": {
    "Identity": {
      "SmsLogin": {
        "CodeTtlSeconds": 300,
        "SendIntervalSeconds": 60,
        "AttemptTtlSeconds": 300,
        "MaxAttempts": 5
      }
    }
  }
}
```

第一版建议：

- 验证码只存短期值，不落库。
- 同一手机号短时间内禁止重复发送。
- 验证码校验通过后立即失效。

## HTTP 调用

Identity 调 Notification 时建议使用 typed `HttpClient`，而不是直接在业务里拼 URL。

当前 Identity 侧已提供 `INotificationClient` 端口和 Infrastructure HTTP 实现，后续短信登录用例可以直接注入该端口。

第一版调用方式：

```csharp
services
    .AddHttpClient<INotificationClient, NotificationClient>()
    .AddLucidServiceDiscovery("notification")
    .AddLucidStandardHttpResilienceHandler(
        configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
```

当前 Identity.Infrastructure 默认使用 Consul 解析 `notification`。Notification 启动时会自动注册到 Consul；本地发码前需要确保 Notification 已启动，并且 Consul 中的 `notification` 实例健康状态为 passing。

## 服务发现

设计约定见 [服务发现约定](../conventions/service-discovery.md)。

建议方向：

- 服务间 HTTP 默认使用 Consul provider。
- 本地开发、单机联调和部署环境保持同一套服务发现语义。
- 后续可按需要接 Kubernetes DNS 或其他环境约定。

服务发现只负责“找到 Notification 在哪里”，不负责重试、超时和熔断。

## Resilience

HTTP 调用 Notification 时建议统一接入 `Lucid:Resilience:Http`。

推荐作用：

- 单次请求超时。
- 失败后重试。
- 下游连续异常时熔断。

不建议：

- 给所有 `HttpClient` 默认套策略。
- 把业务错误伪装成重试成功。

## 第一版接口形态

Identity 当前暴露两个短信登录入口：

```http
POST /api/sms-login/codes
POST /api/sms-login
```

经 Gateway 访问时使用：

```http
POST /api/identity/sms-login/codes
POST /api/identity/sms-login
```

发码请求：

```json
{
  "phoneNumber": "13800138000"
}
```

登录请求：

```json
{
  "phoneNumber": "13800138000",
  "code": "123456"
}
```

`POST /api/sms-login/codes` 成功返回 `204 No Content`。`POST /api/sms-login` 验证通过后返回 token 响应契约。前端和服务器部署场景优先通过 Gateway 路径访问；直连服务路径主要用于本地调试 Identity.Api。

短信发送仍通过 Notification 的最小接口：

```http
POST /internal/notifications
```

请求里包含 `recipient`、`channel`、`subject` 和 `content`。短信登录场景只传手机号、验证码内容和少量上下文，由 Notification 决定最终渠道发送方式。

## 本地验证

本地手动验证步骤已拆到 [短信登录本地 E2E 验证](sms-login-e2e.md)。该文档覆盖数据库迁移、服务启动、发码、取码、登录、失败路径和常见排查。

## 演进顺序

推荐顺序：

1. 先定义短信登录的应用层用例。
2. 再定义 Notification 的 HTTP client 契约。
3. 接入 Redis 验证码和限频。
4. 接入 `Resilience.Http`。
5. 使用服务发现解析 Notification 地址。

这样每一步都能单独验证，不会把多个基础设施同时揉在一起。
