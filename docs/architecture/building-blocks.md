# BuildingBlock 设计规则

本文档定义 LucidMicro 后端 `BuildingBlocks` 的拆分方式、命名规则、依赖方向和项目内部目录结构。

## 核心原则

BuildingBlock 是框架能力边界，不是工具类集合。

每个 BuildingBlock 应代表一个独立能力，例如缓存、业务实体基础类型、持久化、事件总线、认证、序列化、可观测性或 ASP.NET Core 集成。

BuildingBlock 的创建准入规则见 [ADR-0001 后端架构边界与 BuildingBlock 准入规则](../adr/0001-backend-architecture-boundary.md)。

基本规则：

- 能力目录负责组织相关项目。
- 项目后缀表达项目角色。
- 项目内部目录表达代码职责。
- 业务层和应用层优先依赖 `.Abstractions` 或 `.Core`。
- API、Host 或基础设施层负责选择 `.Redis`、`.EFCore`、`.Jwt` 等具体实现。

推荐形态：

```text
BuildingBlocks/
  能力域/
    能力名/
      LucidMicro.BuildingBlocks.能力名.Abstractions/
      LucidMicro.BuildingBlocks.能力名.Core/
      LucidMicro.BuildingBlocks.能力名.具体实现/
```

不是每个能力都必须同时拥有 `.Abstractions`、`.Core` 和具体实现项目。按能力真实需要创建。

## 一级目录收敛目标

`BuildingBlocks` 的一级目录不应随着能力增加而无限平铺。一级目录用于表达能力域，具体能力放在能力域下面。

目标形态：

```text
BuildingBlocks/
  Core/
    Application/
    Domain/

  Web/
    AspNetCore/
    Auth/
    Cors/
    OpenApi/
    RateLimiting/

  Communication/
    Http/
    Resilience/
    ServiceDiscovery/

  Messaging/
    EventBus/
    Outbox/
    Inbox/
    Serialization/

  Data/
    Caching/
    Persistence/
    DistributedLock/

  Operations/
    HealthChecks/
    Logging/
    Observability/
```

能力域边界：

- `Core`：应用层、领域层等架构基础类型。
- `Web`：API Host、ASP.NET Core、认证、OpenAPI、CORS、限流等入口层能力。
- `Communication`：服务间同步通信相关能力，例如 HTTP client、服务发现和 Resilience。
- `Messaging`：异步消息、事件总线、Outbox、Inbox 和消息序列化。
- `Data`：数据访问、缓存、分布式锁等状态基础设施。
- `Operations`：健康检查、日志、可观测性等运行态能力。

目录收敛时只移动物理路径，不同时修改项目名、程序集名或 namespace。示例：

```text
BuildingBlocks/Messaging/Outbox/LucidMicro.BuildingBlocks.Outbox.EFCore/
```

项目仍保持：

```text
LucidMicro.BuildingBlocks.Outbox.EFCore
```

这样可以缩小目录宽度，同时避免无业务价值的大规模命名变更。

## 项目后缀

项目后缀应保持一致：

```text
.Abstractions     接口、契约、Options、轻量 DTO
.Core             纯可复用核心行为
.Redis            Redis 实现
.Memory           内存实现
.EFCore           EF Core 实现
.Jwt              JWT 实现
.RabbitMQ         RabbitMQ 实现
.SystemTextJson   System.Text.Json 实现
.OpenTelemetry    OpenTelemetry 实现
.Http             HTTP 实现
.Static           静态配置实现
.Consul           Consul 实现
.AspNetCore       ASP.NET Core 集成
```

通用依赖方向：

```text
*.Abstractions
  ↑
*.Core
  ↑
*.Redis / *.Memory / *.EFCore / *.Jwt / *.RabbitMQ / *.SystemTextJson / *.OpenTelemetry / *.Http / *.Static / *.Consul / *.AspNetCore
```

如果某个能力只有纯核心行为，没有多种实现，可以只创建 `.Core` 项目。

示例：

```text
Core/
  Domain/
  LucidMicro.BuildingBlocks.Domain.Core/
```

如果某个能力有接口和多个实现，应拆成抽象项目与实现项目。

示例：

```text
Data/
  Caching/
  LucidMicro.BuildingBlocks.Caching.Abstractions/
  LucidMicro.BuildingBlocks.Caching.Redis/
  LucidMicro.BuildingBlocks.Caching.Memory/
```

## 能力域依赖方向

目录收敛后，依赖方向不再只看项目后缀，也要看能力域边界。

通用规则：

- `Core` 是最底层基础能力，只能依赖 `Core` 内项目。
- `Data` 可以依赖 `Core` 和 `Data`。
- `Messaging` 可以依赖 `Messaging`，例如 Outbox/Inbox 依赖 EventBus 抽象。
- `Communication` 可以依赖 `Core` 和 `Communication`。
- `Web` 可以依赖 `Core`、`Data` 和 `Web`，用于认证、审计、异常处理等入口层能力。
- `Operations` 可以依赖运行态需要观测的能力域，例如 RabbitMQ health check 依赖 RabbitMQ event bus 实现。
- 非 `Operations` 项目不应依赖 `Operations`，避免业务能力或基础能力反向依赖运维适配。

矩阵：

```text
Source         Allowed targets
Core           Core
Data           Core, Data
Messaging      Messaging
Communication  Core, Communication
Web            Core, Data, Web
Operations     Core, Data, Messaging, Communication, Web, Operations
```

项目角色规则：

- `.Abstractions` 只能依赖其他 `.Abstractions` 或 `Core` 基础项目。
- `.Core` 可以依赖 `.Abstractions` 或 `Core` 基础项目，不应依赖具体实现。
- 具体实现项目优先依赖本能力的 `.Abstractions` / `.Core`，跨能力依赖应优先依赖对方抽象。
- 具体实现依赖另一个具体实现只允许在少数运行态适配场景出现，例如 `Operations/HealthChecks` 对具体 provider 做 ready check。

这些规则由 BuildingBlocks 架构测试保护。新增 BuildingBlock 或新增项目引用时，应先确认依赖方向能被矩阵解释。

## 通用项目结构

### Abstractions 项目

`.Abstractions` 项目用于放接口、契约、轻量模型、配置对象和常量。它应保持轻量，尽量避免依赖具体中间件或重量级第三方包。

```text
LucidMicro.BuildingBlocks.Xxx.Abstractions/
  LucidMicro.BuildingBlocks.Xxx.Abstractions.csproj

  Contracts/
  Options/
  Models/
  Exceptions/
  Constants/
```

常见内容：

```text
Contracts/       接口、服务契约、能力契约
Options/         配置对象
Models/          轻量模型、返回值、上下文对象
Exceptions/      能力相关异常
Constants/       常量、Header 名、Claim 名等
```

### Core 项目

`.Core` 项目用于放不绑定具体基础设施的默认核心能力。

```text
LucidMicro.BuildingBlocks.Xxx.Core/
  LucidMicro.BuildingBlocks.Xxx.Core.csproj

  Models/
  Services/
  Extensions/
  Options/
  Internal/
```

常见内容：

```text
Models/          核心模型
Services/        默认服务实现
Extensions/      扩展方法
Options/         配置对象
Internal/        不希望业务代码直接依赖的内部实现
```

### 实现项目

实现项目负责接入具体技术栈，例如 Redis、EF Core、JWT、RabbitMQ 或 OpenTelemetry。

```text
LucidMicro.BuildingBlocks.Xxx.Redis/
  LucidMicro.BuildingBlocks.Xxx.Redis.csproj

  DependencyInjection/
  Options/
  Services/
  Internal/
  Extensions/
```

常见内容：

```text
DependencyInjection/   对外注册入口
Options/               实现相关配置对象
Services/              具体服务实现
Internal/              内部适配器、工厂、帮助类
Extensions/            实现相关扩展方法
```

每个实现项目应暴露清晰的注册入口：

```csharp
builder.Services.AddLucidRedisCaching(
    builder.Configuration.GetRequiredSection(LucidRedisCacheOptions.ConfigurationSectionName));
builder.Services.AddLucidJwtAuth(
    builder.Configuration.GetRequiredSection(JwtAccessTokenOptions.ConfigurationSectionName));
builder.Services.AddLucidEfCorePersistence(builder.Configuration);
builder.Services.AddLucidOpenApi(
    builder.Configuration.GetRequiredSection(LucidOpenApiOptions.ConfigurationSectionName));
builder.Services.AddLucidCors(
    builder.Configuration.GetRequiredSection(LucidCorsOptions.ConfigurationSectionName));
builder.Services.AddLucidRateLimiting(
    builder.Configuration.GetRequiredSection(LucidRateLimitingOptions.ConfigurationSectionName));
builder.Services.AddLucidHttpResilience(
    builder.Configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
```

带配置的 BuildingBlock 应由自身负责配置校验和启动失败语义，规则见 [配置与启动校验约定](../conventions/configuration.md)。

## 依赖示例

业务服务的应用层只依赖缓存抽象：

```text
System.Application
  -> LucidMicro.BuildingBlocks.Caching.Abstractions
```

API 启动项目选择 Redis 实现：

```text
System.Api
  -> LucidMicro.BuildingBlocks.Caching.Redis
```

这样测试环境可以替换为内存实现，生产环境可以使用 Redis 实现。

## 创建策略

不要一次性创建所有规划中的 BuildingBlock 项目。

LucidMicro 是框架项目，BuildingBlock 可以先于多个业务服务复用而沉淀。但创建真实项目时，必须形成最小可用闭环：

- 明确的能力边界。
- 至少一个真实实现。
- 清晰的 `AddLucidXxx(...)` 或 `UseLucidXxx(...)` 注册入口。
- 可验证的配置语义和启动失败语义。
- 基础测试覆盖。
- 可供服务和生成器遵循的使用约定。

推荐顺序：

1. 先创建能力目录，例如 `Data/Caching/`、`Core/Domain/`、`Data/Persistence/`。
2. 当能力边界明确，并且能形成最小可用闭环时，再创建对应项目。
3. 优先创建 `.Abstractions` 或 `.Core`，但不要创建没有实现计划的空抽象。
4. 当需要接入具体技术栈时，再创建实现项目。
5. 每个实现项目都要提供 `DependencyInjection/ServiceCollectionExtensions.cs` 作为注册入口。

这能允许框架能力提前沉淀，同时避免空项目、空抽象和无闭环的未来占位。
