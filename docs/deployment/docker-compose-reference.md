# Docker Compose 参考

状态：已落地，作为 Compose 部署的完整参考资料。

如果是首次部署，优先阅读 [Docker Compose 快速部署](docker-compose-quickstart.md)。如果是日常启动、重建、scale 和排查，优先阅读 [Docker Compose 运维操作](docker-compose-operations.md)。

本文档定义 LucidMicro 第一版 Docker Compose 部署方案。

目标是先支持单机部署和联调验证，不在第一版引入 Kubernetes 或完整 CI/CD。当前 app compose 已支持在单机内通过 Consul 和 Gateway 动态发现验证多实例 scale。

## 部署目标

第一版需要部署：

- `identity-api`
- `notification-api`
- `gateway`
- `admin-web`

PostgreSQL、Redis、RabbitMQ 和 Consul 使用独立 infra compose 部署。Consul 用于服务注册与发现，Docker Compose 部署中，业务服务默认使用容器实例信息注册到 Consul。

外部浏览器只访问：

```text
admin-web
gateway
```

业务 API 必须通过 Gateway 进入，不让前端直接访问 Identity 或 Notification。

## 目录规划

推荐后续落地到：

```text
deploy/
  compose/
    app/
      docker-compose.yml
      .env.example
    infra/
      docker-compose.yml
      .env.example
      consul/
        consul.hcl
        lucid-agent-policy.hcl
        lucid-app-policy.hcl
  docker/
    backend/
      Dockerfile
    admin/
      Dockerfile
      nginx.conf
  caddy/
    Caddyfile
```

第一版落地 `deploy/compose/app`、`deploy/compose/infra` 和 `deploy/compose/caddy`。app compose 默认只把 `admin-web` 和 `gateway` 端口绑定到宿主机 `127.0.0.1`，公网 HTTPS 由 Caddy 承接；infra compose 提供 PostgreSQL、Redis、RabbitMQ、Consul、Loki、Tempo、OpenTelemetry Collector、Prometheus 和 Grafana。

## 服务拓扑

```text
Browser
  -> Caddy
       cloud.example.xyz -> admin-web
       api.example.xyz   -> gateway
       /api/identity/*      -> identity-api
       /api/notification/*  -> notification-api

identity-api
  -> postgres
  -> redis
  -> rabbitmq
  -> consul
  -> notification-api via Consul service discovery

notification-api
  -> postgres
  -> rabbitmq
  -> consul

caddy
  -> admin-web
  -> gateway

gateway
  -> consul
  -> identity-api / notification-api via Consul service discovery
```

Compose 内部服务名建议固定为：

```text
consul
postgres
redis
rabbitmq
identity-api
notification-api
gateway
admin-web
caddy
```

## 首次部署流程入口

从零部署流程已经拆到 [Docker Compose 快速部署](docker-compose-quickstart.md)。本参考文档只保留拓扑、环境变量、镜像、端口、操作细节和风险说明。

## Infra Compose 操作

当前 infra compose 包含单节点 Consul、Loki、Tempo、OpenTelemetry Collector、Prometheus 和 Grafana，适合单机服务器部署和联调验证。

infra compose 和 app compose 共用 `lucid-app` 网络。这样 Consul 能通过 Docker DNS 访问 `identity-api`、`notification-api` 做 health check；应用容器也能通过 `http://consul:8500` 访问 Consul API。

Infra 启动、状态查看、端口重建和远程访问命令见 [Docker Compose 运维操作](docker-compose-operations.md)。Consul ACL 首次初始化流程见 [Docker Compose 快速部署](docker-compose-quickstart.md#4-初始化-consul-acl)。

Consul 已启用 ACL，配置文件位于 `deploy/compose/infra/consul/consul.hcl`。Consul Docker entrypoint 启动时会调整 `/consul/config` 的文件权限，因此配置目录不能以只读 volume 挂载。ACL bootstrap 只能执行一次；开发环境如果丢失管理 token，可以删除 Consul volume 后重新初始化，但这会清空 Consul 数据。

Loki 默认只把 HTTP API 绑定到宿主机 `127.0.0.1:3100`。Grafana 默认只绑定到宿主机 `127.0.0.1:3000`，默认账号密码来自 infra `.env`：

```env
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=admin
```

Grafana 已预置 Loki datasource。启用 app compose 的 Loki 日志推送后，可以在 Grafana Explore 中查询：

```text
{app="lucid_micro"}
```

或按服务筛选：

```text
{app="lucid_micro", service="identity"}
```

OpenTelemetry Collector 默认把 OTLP gRPC/HTTP 端口绑定到宿主机 `127.0.0.1:4317` 和 `127.0.0.1:4318`。应用容器通过共享 Docker 网络访问：

```env
OTEL_OTLP_ENDPOINT=http://otel-collector:4317
```

Collector 会把 traces 转发到 Tempo，并把 metrics 暴露给 Prometheus 抓取。Tempo 默认只把 HTTP 查询端口绑定到宿主机 `127.0.0.1:3200`。

Grafana 已预置 Tempo 和 Prometheus datasource。启用 app compose 的 OTLP 导出后，可以在 Grafana Explore 中选择 `Tempo`，按 `service.name` 查询：

```text
LucidMicro.Identity
LucidMicro.Notification
LucidMicro.Gateway
```

也可以从 Loki 日志里的 `TraceId` 复制 trace id，在 Tempo 中直接查询对应链路。

Prometheus 默认只把 HTTP UI/API 绑定到宿主机 `127.0.0.1:9090`。基础 metrics 通过 Collector 的 Prometheus exporter 暴露，Prometheus 默认抓取 `otel-collector:9464`。Collector 会把 OpenTelemetry resource attributes 中的 `service.name` 和 `service.instance.id` 复制为 Prometheus labels，因此在 Grafana Explore 中选择 `Prometheus` 后，可以通过指标自动补全查看 HTTP、HttpClient 和 .NET runtime 指标，并按 `service_name` 和 `service_instance_id` 过滤服务与节点。

```text
{service_name="LucidMicro.Identity"}
count by (service_name, service_instance_id) (http_server_request_duration_seconds_count)
```

Grafana 会自动加载 `LucidMicro` 文件夹下的 `LucidMicro Overview` dashboard。它支持按 `Service` 和 `Instance` 过滤，只使用基础 HTTP、HttpClient、.NET runtime metrics，以及最近日志和 trace 查询入口，不包含业务自定义指标。

### 远程访问 Infra 管理端口

infra compose 默认把 PostgreSQL、Redis、RabbitMQ、Consul、Grafana、Loki、Tempo、Prometheus 和 OpenTelemetry Collector 的宿主机端口绑定到 `127.0.0.1`。这表示：

- 服务器本机可以访问这些端口。
- app 容器仍然可以通过共享 Docker 网络访问 `postgres`、`redis`、`rabbitmq`、`consul`、`loki`、`otel-collector` 等服务名。
- 远程机器不能直接访问这些端口，除非使用 SSH tunnel、VPN、内网地址或把 bind 地址改成 `0.0.0.0`。

本地开发机需要临时访问服务器上的数据库、Redis、RabbitMQ 管理页、Grafana 或 Consul UI 时，优先使用 SSH tunnel，不需要修改 infra `.env`。命令见 [Docker Compose 运维操作](docker-compose-operations.md#远程访问管理端口)。

RabbitMQ 管理页使用 `RABBITMQ_DEFAULT_USER` / `RABBITMQ_DEFAULT_PASS` 登录。Grafana 使用 `GRAFANA_ADMIN_USER` / `GRAFANA_ADMIN_PASSWORD` 登录。Consul UI 使用 ACL bootstrap 生成的管理 token 登录，不是账号密码，也不要使用 app token 登录管理 UI。

开发服务器如果需要临时通过公网或内网直接访问管理端口，可以修改 `deploy/compose/infra/.env` 中对应的 bind 地址。技术上改成 `0.0.0.0` 就会监听宿主机所有网卡：

```env
POSTGRES_BIND=0.0.0.0
REDIS_BIND=0.0.0.0
RABBITMQ_AMQP_BIND=0.0.0.0
RABBITMQ_MANAGEMENT_BIND=0.0.0.0
GRAFANA_HTTP_BIND=0.0.0.0
CONSUL_HTTP_BIND=0.0.0.0
```

如只想开放管理页面，通常只需要开放 `RABBITMQ_MANAGEMENT_BIND`、`GRAFANA_HTTP_BIND` 和 `CONSUL_HTTP_BIND`；PostgreSQL、Redis 和 RabbitMQ AMQP 端口不建议直接暴露到公网。

修改 bind 地址后，需要重建相关容器端口绑定。只改 Consul 或重建全部 infra 容器的命令见 [Docker Compose 运维操作](docker-compose-operations.md#infra-操作)。

不要为了重建端口绑定执行 `down -v`，否则会删除 infra volumes，PostgreSQL、Redis、RabbitMQ、Consul 和 Grafana 的持久化数据都会被清空。

公网访问地址示例：

```text
RabbitMQ UI: http://服务器公网IP:15672
Grafana:     http://服务器公网IP:3000
Consul UI:   http://服务器公网IP:8500
```

直接暴露管理端口只建议用于开发环境或受控内网。公网开放时应至少用服务器防火墙或云安全组限制来源 IP。生产或长期暴露前，优先使用 SSH tunnel、VPN、堡垒机或反向代理加 TLS 和访问控制。Consul HTTP API 不只是 UI，也可以写入服务注册信息；PostgreSQL 和 Redis 即使有密码，也不适合裸露在公网。

应用启动后，可以通过 Consul HTTP API 验证 `identity` 和 `notification` 服务注册结果。Consul UI 登录使用 ACL bootstrap 生成的管理 token。

## 镜像策略

后端第一版可以使用一个通用 Dockerfile，通过构建参数选择项目：

```text
deploy/docker/backend/Dockerfile
```

构建参数示例：

```text
PROJECT_PATH=backend/src/Services/Identity/LucidMicro.Services.Identity.Api/LucidMicro.Services.Identity.Api.csproj
APP_DLL=LucidMicro.Services.Identity.Api.dll
```

需要构建的后端镜像：

- `lucidmicro/identity-api`
- `lucidmicro/notification-api`
- `lucidmicro/gateway`

前端 admin 使用独立 Dockerfile：

```text
deploy/docker/admin/Dockerfile
```

构建时写入：

```text
VITE_API_BASE_URL
```

注意：Vite 环境变量会进入静态构建产物。服务器地址变化时，需要重新构建前端，除非后续改成运行时配置文件。

## 镜像构建命令

从仓库根目录执行。

Identity：

```powershell
docker build `
  -f deploy/docker/backend/Dockerfile `
  --build-arg PROJECT_PATH=backend/src/Services/Identity/LucidMicro.Services.Identity.Api/LucidMicro.Services.Identity.Api.csproj `
  --build-arg APP_DLL=LucidMicro.Services.Identity.Api.dll `
  -t lucidmicro/identity-api:local .
```

Notification：

```powershell
docker build `
  -f deploy/docker/backend/Dockerfile `
  --build-arg PROJECT_PATH=backend/src/Services/Notification/LucidMicro.Services.Notification.Api/LucidMicro.Services.Notification.Api.csproj `
  --build-arg APP_DLL=LucidMicro.Services.Notification.Api.dll `
  -t lucidmicro/notification-api:local .
```

Gateway：

```powershell
docker build `
  -f deploy/docker/backend/Dockerfile `
  --build-arg PROJECT_PATH=backend/src/Gateway/LucidMicro.Gateway/LucidMicro.Gateway.csproj `
  --build-arg APP_DLL=LucidMicro.Gateway.dll `
  -t lucidmicro/gateway:local .
```

Admin：

```powershell
docker build `
  -f deploy/docker/admin/Dockerfile `
  --build-arg VITE_API_BASE_URL=http://localhost:49953 `
  -t lucidmicro/admin-web:local .
```

## 环境变量

应用 compose 环境变量示例位于：

```text
deploy/compose/app/.env.example
```

复制后使用：

```powershell
Copy-Item deploy/compose/app/.env.example deploy/compose/app/.env
```

启动应用 compose 的命令见 [Docker Compose 运维操作](docker-compose-operations.md#app-操作)。

## 应用环境变量

应用 compose 也使用共享网络 `lucid-app`。启动 app 前，需要先完成 Consul ACL bootstrap，并把 app token 写入 `CONSUL_TOKEN`。构建、启动、日志、重启、停止和多实例验证命令见 [Docker Compose 运维操作](docker-compose-operations.md#app-操作)。

应用 compose 不负责数据库迁移。启动 `up -d` 前，必须已经手动完成数据库创建和迁移。

### Identity

必需：

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__Identity=${IDENTITY_CONNECTION_STRING}
Lucid__Caching__Redis__ConnectionString=${REDIS_CONNECTION_STRING}
Lucid__EventBus__RabbitMQ__ConnectionString=${RABBITMQ_CONNECTION_STRING}
Lucid__ServiceDiscovery__Consul__Address=${CONSUL_ADDRESS}
Lucid__ServiceDiscovery__Consul__Token=${CONSUL_TOKEN}
Lucid__ServiceDiscovery__Consul__Registration__ServiceName=identity
Lucid__ServiceDiscovery__Consul__Registration__UseInstanceDefaults=true
Lucid__ServiceDiscovery__Consul__Registration__Port=8080
Lucid__Logging__Serilog__Loki__Enabled=${LOKI_ENABLED}
Lucid__Logging__Serilog__Loki__Uri=${LOKI_URI}
Lucid__Observability__OpenTelemetry__OtlpEndpoint=${OTEL_OTLP_ENDPOINT}
Authentication__Jwt__Issuer=${JWT_ISSUER}
Authentication__Jwt__Audience=${JWT_AUDIENCE}
Authentication__Jwt__RefreshAudience=${JWT_REFRESH_AUDIENCE}
Authentication__Jwt__SigningKey=${JWT_SIGNING_KEY}
```

`JWT_SIGNING_KEY` 必须使用足够长的生产密钥，不使用开发默认值。

### Notification

必需：

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__Notification=${NOTIFICATION_CONNECTION_STRING}
Lucid__EventBus__RabbitMQ__ConnectionString=${RABBITMQ_CONNECTION_STRING}
Lucid__ServiceDiscovery__Consul__Address=${CONSUL_ADDRESS}
Lucid__ServiceDiscovery__Consul__Token=${CONSUL_TOKEN}
Lucid__ServiceDiscovery__Consul__Registration__ServiceName=notification
Lucid__ServiceDiscovery__Consul__Registration__UseInstanceDefaults=true
Lucid__ServiceDiscovery__Consul__Registration__Port=8080
Lucid__Logging__Serilog__Loki__Enabled=${LOKI_ENABLED}
Lucid__Logging__Serilog__Loki__Uri=${LOKI_URI}
Lucid__Observability__OpenTelemetry__OtlpEndpoint=${OTEL_OTLP_ENDPOINT}
Authentication__Jwt__Issuer=${JWT_ISSUER}
Authentication__Jwt__Audience=${JWT_AUDIENCE}
Authentication__Jwt__RefreshAudience=${JWT_REFRESH_AUDIENCE}
Authentication__Jwt__SigningKey=${JWT_SIGNING_KEY}
```

### Gateway

必需：

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Lucid__Gateway__ServiceDiscovery__Enabled=true
Lucid__Gateway__ServiceDiscovery__RefreshIntervalSeconds=10
Lucid__Gateway__ServiceDiscovery__LoadBalancingPolicy=RoundRobin
Lucid__Gateway__ServiceDiscovery__Clusters__identity=identity
Lucid__Gateway__ServiceDiscovery__Clusters__notification=notification
Lucid__ServiceDiscovery__Consul__Address=${CONSUL_ADDRESS}
Lucid__ServiceDiscovery__Consul__Token=${CONSUL_TOKEN}
Lucid__Logging__Serilog__Loki__Enabled=${LOKI_ENABLED}
Lucid__Logging__Serilog__Loki__Uri=${LOKI_URI}
Lucid__Observability__OpenTelemetry__OtlpEndpoint=${OTEL_OTLP_ENDPOINT}
ReverseProxy__Clusters__identity__Destinations__destination1__Address=http://identity-api:8080/
ReverseProxy__Clusters__notification__Destinations__destination1__Address=http://notification-api:8080/
Lucid__Cors__AllowedOrigins__0=${ADMIN_ORIGIN}
Lucid__Cors__AllowedOrigins__1=${LOCAL_ADMIN_ORIGIN}
Lucid__Cors__AllowedOrigins__2=${LOCAL_ADMIN_ORIGIN_127}
Lucid__Cors__AllowedOrigins__3=${LOCAL_ADMIN_ORIGIN_ALT}
Lucid__Cors__AllowedOrigins__4=${LOCAL_ADMIN_ORIGIN_ALT_127}
```

启用 `Lucid:Gateway:ServiceDiscovery` 后，Gateway 会周期性从 Consul 读取 passing 实例并更新 YARP cluster destination。`ReverseProxy__Clusters__...__Address` 仍保留为静态配置兜底和禁用动态发现时的默认地址。

`ADMIN_ORIGIN` 建议配置为已发布前端 `https://cloud.example.xyz`。如果本地 Vite 前端需要访问已发布 Gateway，再保留 `LOCAL_ADMIN_ORIGIN`、`LOCAL_ADMIN_ORIGIN_127`、`LOCAL_ADMIN_ORIGIN_ALT` 和 `LOCAL_ADMIN_ORIGIN_ALT_127`。如果只允许线上前端访问，可以删除本地 origin 或把 compose 中对应环境变量移除。

### Admin

构建时：

```env
VITE_API_BASE_URL=${GATEWAY_PUBLIC_URL}
```

本地 compose 示例可以是：

```env
VITE_API_BASE_URL=http://localhost:49953
```

服务器示例：

```env
VITE_API_BASE_URL=https://api.example.xyz
```

## Caddy 环境变量

Caddy compose 环境变量示例位于：

```text
deploy/compose/caddy/.env.example
```

必需：

```env
CADDY_IMAGE=lucidmicro/caddy-cloudflare:local
CADDY_ACME_EMAIL=admin@example.xyz
CLOUDFLARE_API_TOKEN=Cloudflare API token
CADDY_HTTP_PORT=80
CADDY_HTTPS_PORT=443
```

`CADDY_IMAGE` 默认由 `deploy/docker/caddy/Dockerfile` 构建。该 Dockerfile 基于 Caddy builder 通过 `xcaddy` 加入 `github.com/caddy-dns/cloudflare` 插件，用于 Cloudflare DNS-01 challenge。`CLOUDFLARE_API_TOKEN` 至少需要 `example.xyz` zone 的 `Zone / Zone / Read` 和 `Zone / DNS / Edit` 权限。

Caddyfile 位于：

```text
deploy/caddy/Caddyfile
```

默认域名入口：

```text
https://cloud.example.xyz -> admin-web:80
https://api.example.xyz   -> gateway:8080
```

Caddy 会把证书和账号数据保存到 `caddy_data` volume。该 volume 应持久化保留，避免频繁重新申请证书。Cloudflare DNS-01 challenge 不依赖公网 `80` 端口完成证书签发，但实际访问 HTTPS 仍需要服务器对外开放 `443`。

## 数据库

应用 compose 不创建 PostgreSQL 容器，PostgreSQL 由 infra compose 提供。

infra compose 首次初始化 PostgreSQL volume 时会自动创建两个业务数据库：

```text
lucid_micro_identity
lucid_micro_notification
```

Redis 和 RabbitMQ 同样由 infra compose 提供，并使用独立 volume 持久化数据。

## 数据库迁移策略

第一版建议先使用手动迁移或一次性迁移任务，不把迁移隐式塞进 API 服务启动。

推荐顺序：

1. 启动 infra compose。
2. 确认 PostgreSQL、Redis、RabbitMQ 可访问。
3. 执行 Identity 迁移。
4. 执行 Notification 迁移。
5. 启动 app compose。

当前约定：迁移由部署者手动执行，compose 不包含 migrator 容器。

后续可增加：

```text
identity-migrator
notification-migrator
```

作为 compose profile 中的一次性容器。

暂不建议 API 服务启动时自动迁移数据库，避免多实例启动时竞争迁移。

## 启动依赖

Compose 的 `depends_on` 只能表达启动顺序，不等同于服务已可用。

app compose 不包含 PostgreSQL、Redis、RabbitMQ，因此无法通过 app compose `depends_on` 等待这些基础设施 ready。Consul 也由 infra compose 启动，app 启动前需要先完成 ACL bootstrap 和 app token 配置。

发布前应先独立验证 infra 基础设施可访问，再启动应用服务。infra compose 已包含基础设施 healthcheck：

- `postgres`：`pg_isready`
- `redis`：`redis-cli ping`
- `rabbitmq`：`rabbitmq-diagnostics ping`

## 端口建议

本地 compose：

```text
admin-web       5173 或 8088
gateway         49953
identity-api    不对宿主暴露，必要时 49753
notification-api 不对宿主暴露，必要时 49853
```

服务器部署：

- Caddy compose 默认映射 `80` 和 `443`；防火墙或云安全组至少放行 `443`，如需 HTTP 自动跳转 HTTPS 再放行 `80`。
- `admin-web` 和 `gateway` 默认只绑定宿主机 `127.0.0.1`，也可由 Caddy 通过 Docker 网络直接访问。
- 不对公网暴露 PostgreSQL、Redis、RabbitMQ、业务服务内部端口。

## 验证命令

健康检查、登录、短信发码和通知列表验证命令见 [Docker Compose 运维操作](docker-compose-operations.md#验证命令)。

## 常见风险

- PostgreSQL 数据库未创建，迁移失败。
- RabbitMQ 已启动但尚未 ready，consumer 连接失败。
- Consul 不可访问，服务自注册失败或服务间 HTTP 发现失败。
- Consul 已启用 ACL，但 app `.env` 没有配置 `CONSUL_TOKEN`，服务自注册会收到 403。
- Consul 已启用 ACL，但未设置 agent token，agent 日志会出现 `anonymous token`，critical service 自动注销会被 ACL 拦截。
- JWT signing key 使用了开发默认值。
- Gateway 下游地址仍是 `localhost`，容器内无法访问宿主服务。
- Admin 的 `VITE_API_BASE_URL` 指向了内部服务而不是 Gateway。
- Gateway CORS 未允许 admin 前端域名。
- Cloudflare DNS 未指向服务器公网 IP，或 `CLOUDFLARE_API_TOKEN` 权限不足，Caddy 无法签发证书。
- Cloudflare 橙云代理开启后 SSL/TLS mode 仍是 `Flexible`，可能导致 HTTPS 重定向循环。
- Redis 密码和连接串格式不一致。

## 当前结论

当前 Docker Compose 部署保持这个边界：

- 浏览器只认 Admin 与 Gateway。
- Gateway 通过 Consul 发现内部业务服务，静态内部 DNS 只作为默认配置兜底。
- Identity / Notification 只认基础设施、Consul 和必要的服务间地址。
- 迁移显式执行，不混入 API 服务启动。
