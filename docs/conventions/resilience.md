# Resilience 约定

Resilience 用于约束外部依赖调用的超时、重试和熔断边界。

当前第一版只覆盖 HTTP 调用，不覆盖 EF Core、Redis 或 RabbitMQ。数据库、缓存和消息中间件通常有各自 provider 级策略，统一封装前需要真实接入验证。

Resilience 不负责服务发现。服务发现只负责找到目标地址，Resilience 负责调用失败时的超时、重试和熔断。服务发现约定见 [service-discovery.md](service-discovery.md)。

## 配置

配置节固定为 `Lucid:Resilience:Http`。

```json
{
  "Lucid": {
    "Resilience": {
      "Http": {
        "Enabled": true,
        "TotalRequestTimeoutSeconds": 30,
        "AttemptTimeoutSeconds": 10,
        "Retry": {
          "MaxRetryAttempts": 3,
          "DelayMilliseconds": 200
        },
        "CircuitBreaker": {
          "FailureRatio": 0.5,
          "MinimumThroughput": 20,
          "SamplingDurationSeconds": 30,
          "BreakDurationSeconds": 30
        }
      }
    }
  }
}
```

字段说明：

- `TotalRequestTimeoutSeconds`：一次完整 HTTP 调用的总超时时间，包含重试。
- `AttemptTimeoutSeconds`：单次尝试的超时时间，不能大于总超时时间。
- `Retry.MaxRetryAttempts`：最多重试次数，可以为 `0`。
- `Retry.DelayMilliseconds`：重试间隔。
- `CircuitBreaker.FailureRatio`：熔断失败比例，取值范围 `(0, 1]`。
- `CircuitBreaker.MinimumThroughput`：触发熔断统计前需要的最小吞吐量。
- `CircuitBreaker.SamplingDurationSeconds`：熔断统计窗口。
- `CircuitBreaker.BreakDurationSeconds`：熔断持续时间。

## 注册

服务启动时先注册并校验配置：

```csharp
builder.Services.AddLucidHttpResilience(
    builder.Configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
```

需要 resilience 的 HTTP client 显式添加标准 handler：

```csharp
builder.Services
    .AddHttpClient("SmsProvider")
    .AddLucidStandardHttpResilienceHandler(
        builder.Configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
```

第一版不默认给所有 `HttpClient` 添加策略，避免框架层改变未声明客户端的行为。

## 使用边界

推荐用于：

- 调第三方短信、微信、邮件等 provider。
- Gateway 调下游服务。
- 服务间 HTTP 调用。

暂不用于：

- EF Core 数据库访问。
- Redis 缓存访问。
- RabbitMQ publish / consume。

这些能力后续需要结合真实 provider 行为单独收敛，避免重复重试或隐藏业务错误。
