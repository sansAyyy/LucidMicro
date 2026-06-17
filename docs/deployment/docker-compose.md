# Docker Compose 部署

状态：已落地，面向单机部署和联调验证。

LucidMicro 的 Compose 部署分为两组：

- `deploy/compose/infra`：PostgreSQL、Redis、RabbitMQ、Consul、Loki、Tempo、OpenTelemetry Collector、Prometheus、Grafana。
- `deploy/compose/app`：`identity-api`、`notification-api`、`gateway`、`admin-web`。
- `deploy/compose/caddy`：Caddy 统一公网入口，自动 HTTPS，代理到 `admin-web` 和 `gateway`。

浏览器只访问 Admin 和 Gateway。服务器域名部署时，公网入口由 Caddy 承接：

```text
https://cloud.example.xyz -> admin-web
https://api.example.xyz   -> gateway
```

业务 API 统一从 Gateway 进入：

```text
/api/identity/*      -> identity-api
/api/notification/*  -> notification-api
```

Identity、Notification 不建议直接暴露到公网。

## 阅读路径

- 首次部署：阅读 [Docker Compose 快速部署](docker-compose-quickstart.md)。
- 日常运维：阅读 [Docker Compose 运维操作](docker-compose-operations.md)。
- 环境变量、拓扑、镜像、端口、风险清单：阅读 [Docker Compose 参考](docker-compose-reference.md)。
- GitHub Actions 自动部署：阅读 [GitHub Actions 部署](github-actions.md)。

## 当前边界

- 支持单机部署和联调验证。
- 支持通过 Consul 和 Gateway 验证多实例 scale。
- 支持通过 Caddy 暴露 HTTPS 域名入口。
- 数据库迁移由部署者显式执行，app compose 不自动迁移。
- Kubernetes 和完整 CI/CD 暂不属于当前 Compose 部署闭环。

## 默认入口

```text
Admin:   http://localhost:8088
Gateway: http://localhost:49953
Caddy:   https://cloud.example.xyz / https://api.example.xyz
```

默认管理员在 Identity 数据库迁移后创建：

```text
admin / Admin@123456
```

生产环境首次登录后应立即修改默认密码，并替换 `.env` 中所有生产敏感值。

