# Contracts 边界规则

本文档定义 `backend/src/Contracts` 的职责边界、依赖方向和项目结构。

Contracts 是跨服务共享契约，不是共享业务库。

## 职责

Contracts 用于放跨服务调用必须共享的稳定模型：

- Integration Events
- HTTP request / response DTO
- 跨服务常量，例如 channel、status、event name
- 轻量 value object 或 enum-like 常量

Contracts 不放：

- 业务服务实现
- ApplicationService
- Domain Entity
- Repository / DbContext
- HTTP client 实现
- Provider / SDK adapter
- 配置注册入口

## 项目结构

契约项目按业务边界拆分：

```text
backend/src/Contracts/
  LucidMicro.Contracts.Notification/
    Constants/
    Http/
      Requests/
      Responses/
    IntegrationEvents/
```

不要把所有服务契约放进一个大 `LucidMicro.Contracts` 项目。

## 依赖方向

Contracts 项目应保持轻量：

```text
Services -> Contracts
Contracts -> BuildingBlocks *.Abstractions
Contracts -> Contracts
```

允许：

- 引用其他 Contracts 项目。
- 引用极轻量 BuildingBlock 抽象，例如 `EventBus.Abstractions`，用于共享集成事件基类和事件名约定。

禁止：

- 引用任何 `Services/*` 项目。
- 引用 BuildingBlock 具体实现，例如 `.EFCore`、`.Redis`、`.RabbitMQ`、`.AspNetCore`、`.OpenTelemetry`。
- 引用 ASP.NET Core、EF Core、RabbitMQ、Redis 等基础设施包。
- 在 Contracts 中隐藏业务规则或服务编排逻辑。

如果契约开始需要复杂行为，优先把行为留在拥有该契约的服务内，Contracts 只保留稳定输入输出模型。

## 版本策略

契约变化默认向后兼容：

- 新增字段优先使用可空或有默认语义的字段。
- 不直接删除已发布字段。
- 不改变已有字段含义。
- Integration Event 名称带版本，例如 `notification.send-requested.v1`。

需要破坏性变更时，新建 v2 契约或 v2 event name，让消费者逐步迁移。

## 测试

Contracts 边界由架构测试保护：

- Contracts 不依赖 Services。
- Contracts 只依赖其他 Contracts 或 BuildingBlock 抽象项目。
- Contracts 不直接引用 NuGet 包。

契约内容应补稳定性测试，例如 JSON 字段名、Integration Event name 和常量值。
