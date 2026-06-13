# ADR-0001 后端架构边界与 BuildingBlock 准入规则

## 状态

Accepted

## 日期

2026-05-27

## 背景

LucidMicro 的目标不是单一业务系统，而是面向微服务应用的快速开发框架。

因此，后端允许先于具体业务规模沉淀框架能力，例如认证、持久化、事件总线、可观测性、日志、Outbox、Inbox、OpenAPI、CORS 和健康检查。

但框架优先不等于可以提前创建空抽象、空项目或仅为未来可能性保留占位实现。随着 BuildingBlock 数量增加，需要明确后端架构边界和新增能力的准入规则，避免框架演进变成无约束的项目拆分。

## 决策

LucidMicro 后端继续采用框架优先的设计方向。

新增 BuildingBlock 不要求先被多个业务服务复用。只要它代表明确的框架能力，并能形成最小可用闭环，就可以独立沉淀。

一个 BuildingBlock 的最小闭环包括：

- 明确的能力边界：解决认证、缓存、持久化、消息、日志、可观测性等框架问题，而不是杂项工具集合。
- 至少一个真实实现：不创建只有接口、没有实现的占位项目。
- 清晰的宿主接入方式：提供 `AddLucidXxx(...)` 或 `UseLucidXxx(...)` 等注册入口。
- 可验证的配置语义：配置缺失、配置非法和启动失败行为必须明确。
- 基础测试覆盖：至少覆盖配置校验、注册入口、核心行为或关键失败路径。
- 文档或约定说明：新增能力应能被后续服务和代码生成流程一致使用。

服务默认保持四层结构：

```text
ServiceName.Api
ServiceName.Application
ServiceName.Domain
ServiceName.Infrastructure
```

当前服务模板不采用 CQRS、MediatR、Command Handler、Query Handler 作为默认结构。

Controller 直接调用 ApplicationService。ApplicationService 按 Feature 聚合用例，不按单个 CRUD 操作机械拆分服务。

Repository 和 Specification 只作为常见持久化访问约定。复杂查询、报表查询、跨聚合读取或明显更适合 EF Core 表达的场景，可以在 Infrastructure 层直接使用 DbContext 或专用查询对象实现。

## 不采用

以下做法不作为默认方向：

- 为未来可能支持的技术栈提前创建空项目，例如没有真实需求和实现的 `.Memory`、`.Kafka`、`.Mongo`、`.ElasticSearch`。
- 为每个实体创建空 Repository。
- 将 CQRS、MediatR 或 Pipeline 行为作为服务模板默认结构。
- 把所有扩展方法、工具函数或一次性封装都提升为 BuildingBlock。
- 仅因为某个能力很小，就无条件拆成独立 csproj；独立项目应有依赖隔离、实现替换、宿主接入或框架能力边界上的理由。

## 后果

这个决策允许 LucidMicro 按框架产品的方式主动建设后端能力，而不是等待多个业务服务复用后才抽象。

代价是后端项目数量会高于普通业务系统。为了控制复杂度，每个 BuildingBlock 都必须能说明自己的能力边界，并具备实现、注册、配置、测试和使用约定。

后续新增 BuildingBlock 时，应优先回答：

1. 这是框架能力，还是某个服务的局部技术细节？
2. 是否已经有一个可工作的真实实现？
3. 宿主服务如何接入？
4. 配置错误时如何失败？
5. 有没有测试证明核心行为可用？

如果这些问题无法回答，应先保留在具体服务或规划文档中，不创建真实 BuildingBlock 项目。
