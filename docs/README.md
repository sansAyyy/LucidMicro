# LucidMicro 文档

这里是 LucidMicro 的文档总目录。第一次接触项目时，建议先从“快速开始”和“部署”读起；需要扩展服务或 BuildingBlock 时，再进入架构和约定文档。

## 快速开始

- [本地开发快速开始](development/local-setup.md)
- [数据库迁移](development/database-migrations.md)
- [Admin 前端](frontend/admin.md)

## 部署

- [Docker Compose 部署](deployment/docker-compose.md)
- [Docker Compose 快速部署](deployment/docker-compose-quickstart.md)
- [Docker Compose 运维操作](deployment/docker-compose-operations.md)
- [Docker Compose 参考](deployment/docker-compose-reference.md)

## 架构

- [架构原则](architecture/principles.md)
- [仓库结构](architecture/repository-structure.md)
- [服务模板结构规则](architecture/service-structure.md)
- [BuildingBlock 设计规则](architecture/building-blocks.md)
- [服务契约边界](architecture/contracts.md)
- [Gateway 设计](architecture/gateway.md)
- [权限模型](architecture/permissions.md)

## 后端约定

- [API 约定](conventions/api.md)
- [OpenAPI 约定](conventions/openapi.md)
- [API Client 生成策略](conventions/api-client.md)
- [认证约定](conventions/authentication.md)
- [CORS 约定](conventions/cors.md)
- [配置与启动校验约定](conventions/configuration.md)
- [缓存约定](conventions/caching.md)
- [分布式锁约定](conventions/distributed-locking.md)
- [服务发现约定](conventions/service-discovery.md)
- [服务注册约定](conventions/service-registration.md)
- [服务间 HTTP 调用约定](conventions/service-to-service-http.md)
- [Resilience 约定](conventions/resilience.md)
- [Messaging 约定](conventions/messaging.md)
- [Outbox 约定](conventions/outbox.md)
- [Inbox 约定](conventions/inbox.md)
- [限流约定](conventions/rate-limiting.md)
- [可观测性约定](conventions/observability.md)

## 服务

- [Notification 服务设计](services/notification.md)
- [Notification 本地 E2E 验证](services/notification-e2e.md)
- [短信登录技术设计](services/sms-login.md)
- [短信登录本地 E2E 验证](services/sms-login-e2e.md)

## ADR

- [ADR-0001 后端架构边界与 BuildingBlock 准入规则](adr/0001-backend-architecture-boundary.md)

