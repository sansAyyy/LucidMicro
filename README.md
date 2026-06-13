# LucidMicro

LucidMicro 是一个面向微服务应用的快速开发框架。当前仓库已落地 .NET 后端、YARP Gateway、Vue 3 管理端、Caddy HTTPS 入口、Docker Compose 单机部署，以及 Redis、RabbitMQ、Consul、OpenTelemetry 等基础设施接入。

规划或占位中的能力包括 uni-app 移动端/H5、Kubernetes 清单、前端共享包和更完整的脚手架模板。

## 当前能力

- 后端：.NET、分层服务结构、BuildingBlocks、Identity、Notification、Gateway。
- 前端：Vue 3、TypeScript、Vite、Pinia、Vue Router、Element Plus 管理端。
- 基础设施：PostgreSQL、Redis、RabbitMQ、Consul、Loki、Tempo、Prometheus、Grafana。
- 部署：`deploy/compose/infra`、`deploy/compose/app` 和 `deploy/compose/caddy` 支持单机联调、部署和 HTTPS 域名入口。
- 模板：已提供后端 CRUD 模块模板和 `scripts/new-crud.ps1`。

## 快速开始

本地开发建议先看 [本地开发快速开始](docs/development/local-setup.md)。

Docker Compose 部署建议先看 [Docker Compose 部署](docs/deployment/docker-compose.md)。

默认入口：

```text
Admin:   http://localhost:8088
Gateway: http://localhost:49953
Caddy:   https://cloud.example.xyz / https://api.example.xyz
```

默认管理员在 Identity 数据库迁移后创建：

```text
admin / Admin@123456
```

生产环境首次登录后应立即修改默认密码。

## 常用命令

后端测试：

```powershell
dotnet test backend/LucidMicro.slnx
```

Admin 本地开发：

```powershell
cd frontend
corepack pnpm install
corepack pnpm dev:admin
```

Admin 构建：

```powershell
cd frontend
corepack pnpm build:admin
```

Docker Compose 部署入口：

```powershell
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml up -d
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --build
docker compose --env-file deploy/compose/caddy/.env -f deploy/compose/caddy/docker-compose.yml up -d --build
```

## 文档导航

按目标阅读：

- 浏览完整文档目录：[LucidMicro 文档](docs/README.md)
- 跑本地环境：[本地开发快速开始](docs/development/local-setup.md)
- 部署到服务器：[Docker Compose 部署](docs/deployment/docker-compose.md)
- 了解整体设计：[架构原则](docs/architecture/principles.md)
- 新增后端服务或模块：[服务模板结构规则](docs/architecture/service-structure.md)
- 新增 BuildingBlock：[BuildingBlock 设计规则](docs/architecture/building-blocks.md)
- 管理端开发：[Admin 前端](docs/frontend/admin.md)

核心架构文档：

- [架构原则](docs/architecture/principles.md)
- [BuildingBlock 设计规则](docs/architecture/building-blocks.md)
- [仓库结构](docs/architecture/repository-structure.md)
- [Gateway 设计](docs/architecture/gateway.md)
- [权限模型](docs/architecture/permissions.md)
