# Rate Limiting 约定

LucidMicro 使用 `LucidMicro.BuildingBlocks.RateLimiting.AspNetCore` 封装 ASP.NET Core 内置限流能力。

当前只提供最小全局固定窗口策略，不做业务接口接入。

## 使用策略

限流分两层：

- Gateway 层：保护外部入口，适合做 IP、客户端、路径维度限流。
- 服务层：保护敏感业务能力，适合登录、验证码、通知发送等接口。

当前阶段先实现 BuildingBlock，不接入具体服务。等 Gateway 或具体敏感接口落地后，再决定限流粒度。

## 配置

配置统一放在 `Lucid:RateLimiting`：

```json
{
  "Lucid": {
    "RateLimiting": {
      "Enabled": true,
      "PermitLimit": 100,
      "WindowSeconds": 60,
      "QueueLimit": 0,
      "RejectionStatusCode": 429
    }
  }
}
```

字段含义：

- `Enabled`：是否启用限流中间件。
- `PermitLimit`：每个窗口允许的请求数。
- `WindowSeconds`：固定窗口秒数。
- `QueueLimit`：超过限制后允许排队的请求数，默认 `0`。
- `RejectionStatusCode`：拒绝请求时返回的 HTTP 状态码，默认 `429`。

当 `Enabled` 为 `true` 时，`PermitLimit`、`WindowSeconds` 必须大于 `0`，`QueueLimit` 不能小于 `0`，`RejectionStatusCode` 必须是 `4xx` 或 `5xx`。

## 服务接入

API 项目引用：

```text
LucidMicro.BuildingBlocks.RateLimiting.AspNetCore
```

注册：

```csharp
builder.Services.AddLucidRateLimiting(
    builder.Configuration.GetRequiredSection(LucidRateLimitingOptions.ConfigurationSectionName));
```

中间件：

```csharp
app.UseExceptionHandler();
app.UseLucidSerilogRequestLogging();
app.UseLucidCors();
app.UseLucidRateLimiting();

app.UseAuthentication();
app.UseAuthorization();
```

`UseLucidRateLimiting` 应放在 CORS 之后、认证授权之前。

## 后续演进

第一版保持克制，只做全局固定窗口。

后续真实接入时再考虑：

- 按 IP、用户、租户或 client id 分区。
- endpoint 级命名 policy。
- 登录、验证码等敏感接口的单独策略。
- Gateway 层统一入口限流。
- 分布式限流。
