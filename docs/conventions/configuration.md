# 配置与启动校验约定

LucidMicro 的配置校验归属对应 BuildingBlock，不设中央配置校验器。

每个 BuildingBlock 最了解自己的配置语义。配置是否合法、哪些字段必填、哪些组合不允许，都应由该 BuildingBlock 的注册入口或 Options 类型负责。

## 基本规则

自定义 Lucid 配置默认放在 `Lucid:*` 下。

例外：

- ASP.NET Core 或第三方生态已经有稳定顶层配置时，可以保留其标准节，例如 `Serilog`。
- 认证配置当前沿用 `Authentication:Jwt`，与 ASP.NET Core authentication 语义保持接近。

每个有配置的 BuildingBlock 应提供：

```csharp
public const string ConfigurationSectionName = "Lucid:...";
```

服务接入时应使用：

```csharp
builder.Services.AddLucidXxx(
    builder.Configuration.GetRequiredSection(XxxOptions.ConfigurationSectionName));
```

如果注册入口不是基于 `IConfigurationSection`，也应在注册时执行等价的 eager validation。

## 校验边界

推荐模式：

```csharp
services
    .AddOptions<XxxOptions>()
    .Bind(configurationSection)
    .Validate(ValidateOptions, "Lucid Xxx options are invalid.")
    .ValidateOnStart();
```

如果注册时必须立即读取配置创建基础设施对象，也可以先 bind 一次并立即校验：

```csharp
var options = configurationSection.Get<XxxOptions>() ?? new XxxOptions();
options.Validate();
```

这种情况下仍建议在条件成熟时补充 `AddOptions<T>().ValidateOnStart()`，方便统一宿主启动语义。

不要把某个 BuildingBlock 的配置规则放进另一个 BuildingBlock，也不要新增中央“大总管”来理解所有配置。

## 当前复盘

| BuildingBlock | 配置节 | 当前校验方式 | 结论 |
| --- | --- | --- | --- |
| Auth.AspNetCore JWT | `Authentication:Jwt` | eager validation + `ValidateOnStart()` | 已达标，保留认证语义例外 |
| Cors.AspNetCore | `Lucid:Cors` | eager validation + `ValidateOnStart()` | 已达标 |
| RateLimiting.AspNetCore | `Lucid:RateLimiting` | eager validation + `ValidateOnStart()` | 已达标 |
| OpenApi.AspNetCore | `Lucid:OpenApi` | `ValidateOnStart()` | 已达标 |
| Observability.OpenTelemetry | `Lucid:Observability:OpenTelemetry` | eager validation + `ValidateOnStart()` | 已达标 |
| Logging.Serilog | `Lucid:Logging:Serilog` + `Serilog` | 注册时 eager `Validate()`，Loki/File 子配置按启用状态校验 | 可接受，顶层 `Serilog` 属官方配置 |
| Caching.Redis | `Lucid:Caching:Redis` | eager validation + `ValidateOnStart()` | 已达标 |
| EventBus.RabbitMQ | `Lucid:EventBus:RabbitMQ` | eager validation + `ValidateOnStart()` | 已达标 |
| Resilience.Http | `Lucid:Resilience:Http` | eager validation + `ValidateOnStart()` | 已达标 |
| ServiceDiscovery.Static | `Lucid:ServiceDiscovery` | eager validation + `ValidateOnStart()` | 已达标 |
| ServiceDiscovery.Consul | `Lucid:ServiceDiscovery:Consul` | eager validation + `ValidateOnStart()` | 已达标 |
| ServiceDiscovery.Consul Registration | `Lucid:ServiceDiscovery:Consul:Registration` | eager validation + `ValidateOnStart()` | 已达标 |
| HealthChecks.Consul | 复用 `Lucid:ServiceDiscovery:Consul` | 依赖 ServiceDiscovery.Consul options 校验 | 已达标 |
| Outbox.Core | 代码配置 | 注册时 eager `Validate()` | 可接受，当前不是配置文件驱动 |
| Outbox.EFCore | 代码配置 | 注册时 eager `Validate()` | 可接受，当前不是配置文件驱动 |
| Inbox.EFCore | 无配置 | 不需要 | 已达标 |
| HealthChecks.* | 复用依赖配置或无配置 | 依赖对应 BuildingBlock 校验 | 已达标 |

## 新增 BuildingBlock 检查清单

新增或修改带配置的 BuildingBlock 时，需要确认：

- Options 类型有 `ConfigurationSectionName`。
- 配置节名称符合 `Lucid:*`，除非有明确生态例外。
- 必填字段和非法组合有测试覆盖。
- 注册入口会在启动阶段暴露配置错误。
- 文档说明配置节、示例和中间件顺序。
