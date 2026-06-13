# LucidMicro 架构原则

本文档记录 LucidMicro 的整体架构方向，供项目贡献者和 Codex 辅助开发共同遵循。

## 目标

LucidMicro 不只是一个业务系统，而是一个用于构建微服务应用的快速开发框架。

这套架构需要支持：

- 快速创建新服务、新模块、CRUD 流程和前端功能。
- 在领域层、应用层、基础设施层和交付层之间保持清晰的依赖边界。
- 支持可替换的基础设施实现，例如 Redis、EF Core、JWT、RabbitMQ 和 OpenTelemetry。
- 通过共享约定，让 Codex 能长期生成一致的代码。
- 支持基于 Caddy、容器的部署，并在需要时扩展到 Kubernetes。

## 仓库风格

LucidMicro 使用 monorepo。

这个 monorepo 包含后端服务、前端应用、可复用前端包、部署资源、脚手架模板、脚本和架构文档。

顶层目录应保持稳定：

```text
backend/       .NET 解决方案、BuildingBlocks、服务、测试
frontend/      Vue 3 管理端、移动端占位、共享前端包规划
deploy/        Docker、compose、Caddy，以及 Kubernetes 占位
templates/     CRUD 模板，以及服务、前端功能、uni-app 页面模板规划
scripts/       开发、构建、测试、生成器脚本
docs/          架构、约定和 ADR
```

## 后端原则

后端目标版本为 .NET 10。

后端代码围绕可复用 BuildingBlock 和独立微服务组织。

每个服务应遵循分层结构：

```text
ServiceName.Api
ServiceName.Application
ServiceName.Domain
ServiceName.Infrastructure
```

服务项目内部目录、四层职责、测试结构和创建策略见 [服务模板结构规则](service-structure.md)。
服务启动注册边界见 [服务启动注册约定](../conventions/service-registration.md)。

推荐的依赖方向是：

```text
Api -> Application -> Domain
Infrastructure -> Application / Domain
```

应用层代码应依赖抽象和核心领域类型。具体基础设施实现应由 API Host 或应用宿主项目选择。

## BuildingBlock 原则

BuildingBlock 是框架能力，不是杂项工具目录。

每个 BuildingBlock 都应是独立的，并代表一种明确能力，例如缓存、持久化、事件总线、认证、序列化或可观测性。

更完整的目录规则、项目后缀、项目内部结构和初始能力清单见 [BuildingBlock 设计规则](building-blocks.md)。

后端架构边界和 BuildingBlock 准入规则见 [ADR-0001 后端架构边界与 BuildingBlock 准入规则](../adr/0001-backend-architecture-boundary.md)。

LucidMicro 作为框架，可以先于具体业务规模沉淀 BuildingBlock，但真实项目必须形成最小可用闭环：明确能力边界、至少一个真实实现、清晰注册入口、可验证配置语义、基础测试覆盖和使用约定。不要创建只有规划价值、没有真实实现的占位项目。

当某个能力包含接口，并且可能存在多种实现时，应拆分为独立项目：

```text
BuildingBlocks/Data/Caching/
  LucidMicro.BuildingBlocks.Caching.Abstractions
  LucidMicro.BuildingBlocks.Caching.Redis
  LucidMicro.BuildingBlocks.Caching.Memory
```

业务层和应用层依赖抽象项目。启动项目或宿主项目负责选择具体实现。

示例：

```text
System.Application
  -> LucidMicro.BuildingBlocks.Caching.Abstractions

System.Api
  -> LucidMicro.BuildingBlocks.Caching.Redis
```

如果某个能力是纯核心行为，并且不需要多种实现，使用单个 `Core` 项目即可。

示例：

```text
BuildingBlocks/Core/Domain/
  LucidMicro.BuildingBlocks.Domain.Core
```

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

通用依赖规则：

```text
*.Abstractions
  ↑
*.Core
  ↑
*.Redis / *.EFCore / *.Jwt / *.RabbitMQ / *.SystemTextJson / *.OpenTelemetry / *.Http / *.Static / *.Consul / *.AspNetCore
```

抽象项目应保持轻量。除非有充分理由，否则不要把实现细节或重量级第三方依赖放进抽象项目。

每个实现项目都应暴露清晰的注册入口：

```csharp
builder.Services.AddLucidRedisCaching(
    builder.Configuration.GetRequiredSection(LucidRedisCacheOptions.ConfigurationSectionName));
builder.Services.AddLucidJwtAuth(
    builder.Configuration.GetRequiredSection(JwtAccessTokenOptions.ConfigurationSectionName));
builder.Services.AddLucidEfCorePersistence(builder.Configuration);
```

## 建议的 BuildingBlock

初始 BuildingBlock 建议如下：

```text
Core/
  Application/
    LucidMicro.BuildingBlocks.Application/

  Domain/
    LucidMicro.BuildingBlocks.Domain.Core/

Data/
  Caching/
    LucidMicro.BuildingBlocks.Caching.Abstractions/
    LucidMicro.BuildingBlocks.Caching.Redis/
    LucidMicro.BuildingBlocks.Caching.Memory/

  Persistence/
    LucidMicro.BuildingBlocks.Persistence.Abstractions/
    LucidMicro.BuildingBlocks.Persistence.EFCore/

  DistributedLock/
    LucidMicro.BuildingBlocks.DistributedLock.Abstractions/
    LucidMicro.BuildingBlocks.DistributedLock.Redis/

Messaging/
  EventBus/
    LucidMicro.BuildingBlocks.EventBus.Abstractions/
    LucidMicro.BuildingBlocks.EventBus.RabbitMQ/

  Outbox/
    LucidMicro.BuildingBlocks.Outbox.Abstractions/
    LucidMicro.BuildingBlocks.Outbox.Core/
    LucidMicro.BuildingBlocks.Outbox.EFCore/

  Inbox/
    LucidMicro.BuildingBlocks.Inbox.Abstractions/
    LucidMicro.BuildingBlocks.Inbox.Core/
    LucidMicro.BuildingBlocks.Inbox.EFCore/

  Serialization/
    LucidMicro.BuildingBlocks.Serialization.Abstractions/
    LucidMicro.BuildingBlocks.Serialization.SystemTextJson/

Communication/
  Http/
    LucidMicro.BuildingBlocks.Http.Core/

  Resilience/
    LucidMicro.BuildingBlocks.Resilience.Http/

  ServiceDiscovery/
    LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions/
    LucidMicro.BuildingBlocks.ServiceDiscovery.Static/
    LucidMicro.BuildingBlocks.ServiceDiscovery.Consul/
    LucidMicro.BuildingBlocks.ServiceDiscovery.Http/

Web/
  AspNetCore/
    LucidMicro.BuildingBlocks.AspNetCore.Core/

  Auth/
    LucidMicro.BuildingBlocks.Auth.Abstractions/
    LucidMicro.BuildingBlocks.Auth.AspNetCore/

  OpenApi/
    LucidMicro.BuildingBlocks.OpenApi.AspNetCore/

  Cors/
    LucidMicro.BuildingBlocks.Cors.AspNetCore/

  RateLimiting/
    LucidMicro.BuildingBlocks.RateLimiting.AspNetCore/

Operations/
  HealthChecks/
    LucidMicro.BuildingBlocks.HealthChecks.AspNetCore/
    LucidMicro.BuildingBlocks.HealthChecks.Npgsql/
    LucidMicro.BuildingBlocks.HealthChecks.Redis/
    LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ/

  Logging/
    LucidMicro.BuildingBlocks.Logging.Serilog/

  Observability/
    LucidMicro.BuildingBlocks.Observability.OpenTelemetry/
```

不要一次性创建所有规划中的项目。可以先创建目录边界；当某个框架能力能够满足 ADR-0001 的最小闭环时，再创建对应真实项目。没有实现、注册入口、配置语义和测试覆盖的能力，应先保留在规划文档中。

## 前端原则

前端使用 workspace 管理应用。当前已落地 Admin，Mobile 仍是占位目录：

- `admin`：Vue 3 管理端应用。
- `mobile`：uni-app H5 和小程序应用，当前未落地真实代码。

两个应用都应使用 Vue 3、Composition API、TypeScript，并按功能组织代码。

管理端应用结构：

```text
src/
  app/
  router/
  stores/
  features/
    user/
      api/
      components/
      composables/
      pages/
      types/
  shared/
    api/
    components/
    composables/
    constants/
    utils/
```

移动端应用结构：

```text
src/
  app/
  pages/
  features/
    order/
      pages/
      components/
      composables/
      api/
      types/
  shared/
    api/
    components/
    composables/
    utils/
  stores/
pages.json
manifest.json
uni.scss
```

共享前端包应放在 `frontend/packages` 下。

推荐的包边界：

```text
api-client/       类型化 HTTP 客户端和生成的 API 客户端
shared-types/     共享 TypeScript 类型
shared-ui/        确实存在跨应用复用价值的 UI 组件
shared-config/    ESLint、TypeScript、Vite 和环境约定
```

## 部署原则

Caddy 属于部署层，不属于后端或前端源码。

部署文件应放在 `deploy` 下：

```text
deploy/
  caddy/
  docker/
  compose/
  k8s/
```

Caddy 是统一域名、TLS 和部署层反向代理入口；当前已落地的浏览器 API 入口是 Gateway。

Gateway 的职责边界、路由、CORS 收口、OpenAPI 和服务发现策略见 [Gateway 设计](gateway.md)。

## 模板原则

因为 LucidMicro 是快速开发框架，模板是一等资产。

模板应支持生成：

- 新后端服务。
- 新 CRUD 模块。
- 新 Vue 管理端功能。
- 新 uni-app 页面或功能模块。

模板放在 `templates` 下。

生成器脚本放在 `scripts` 下。

## 文档原则

文档应放在 Codex 和贡献者都能快速找到的位置：

```text
docs/architecture/    稳定的架构和仓库形态
docs/conventions/     编码、命名、API、前端和 Git 约定
docs/adr/             架构决策记录
```

架构文档描述系统的目标形态。ADR 记录已经做出的架构决策以及背后的原因。

当决策发生变化时，应更新相关架构文档；如果变化影响长期方向，还应新增 ADR。
