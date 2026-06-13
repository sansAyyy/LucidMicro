# Docker Compose 快速部署

状态：已落地，适合新服务器首次部署。

以下命令从仓库根目录执行。服务器需要先安装 Docker、Docker Compose plugin、Git。当前数据库迁移需要手动执行，因此执行迁移的机器还需要安装项目匹配的 .NET SDK 和 `dotnet-ef`。

如果使用默认 Caddy 配置，先在 Cloudflare DNS 中创建两条记录并指向服务器公网 IP：

```text
cloud.example.xyz  A  服务器公网IP
api.example.xyz    A  服务器公网IP
```

默认 Caddy 镜像会内置 `caddy-dns/cloudflare` 插件，并通过 Cloudflare DNS-01 challenge 自动申请和续期 HTTPS 证书。Cloudflare 记录可以使用 DNS only，也可以开启橙云代理；开启橙云代理时，建议把 Cloudflare SSL/TLS mode 设为 `Full (strict)`，不要使用 `Flexible`。

在 Cloudflare 创建一个 API token，至少授予 `example.xyz` 这个 zone 的 `Zone / Zone / Read` 和 `Zone / DNS / Edit` 权限。该 token 只写入服务器上的 `deploy/compose/caddy/.env`，不要提交到仓库。

## 1. 创建共享网络

```bash
docker network inspect lucid-app >/dev/null 2>&1 || docker network create lucid-app
```

## 2. 准备 Infra 环境变量

```bash
cp deploy/compose/infra/.env.example deploy/compose/infra/.env
```

至少修改以下生产敏感值：

```env
POSTGRES_PASSWORD=换成强密码
REDIS_PASSWORD=换成强密码
RABBITMQ_DEFAULT_PASS=换成强密码
GRAFANA_ADMIN_PASSWORD=换成强密码
```

服务器部署时，除非已经准备好防火墙、VPN 或内网访问控制，infra `.env` 中的 `*_BIND` 默认保持 `127.0.0.1`，不要直接把 PostgreSQL、Redis、RabbitMQ、Consul 等管理端口暴露到公网。

## 3. 启动 Infra

```bash
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml up -d
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml ps
```

infra compose 首次初始化 PostgreSQL volume 时，会自动创建 `lucid_micro_identity` 和 `lucid_micro_notification` 两个业务数据库。

## 4. 初始化 Consul ACL

Consul 首次启动后需要 bootstrap 一次，生成管理 token：

```bash
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml exec -T consul consul acl bootstrap | tee consul-bootstrap-token.txt
export CONSUL_HTTP_TOKEN=$(awk '/SecretID/ { print $2 }' consul-bootstrap-token.txt)
```

创建 agent token：

```bash
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml exec -T consul consul acl policy create \
  -name lucid-agent \
  -rules @/consul/config/lucid-agent-policy.hcl \
  -token "$CONSUL_HTTP_TOKEN"

docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml exec -T consul consul acl token create \
  -description "Lucid Consul agent" \
  -policy-name lucid-agent \
  -token "$CONSUL_HTTP_TOKEN" | tee consul-lucid-agent-token.txt

export CONSUL_AGENT_TOKEN=$(awk '/SecretID/ { print $2 }' consul-lucid-agent-token.txt)

docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml exec -T consul consul acl set-agent-token \
  -token "$CONSUL_HTTP_TOKEN" \
  agent "$CONSUL_AGENT_TOKEN"
```

创建应用 token：

```bash
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml exec -T consul consul acl policy create \
  -name lucid-app \
  -rules @/consul/config/lucid-app-policy.hcl \
  -token "$CONSUL_HTTP_TOKEN"

docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml exec -T consul consul acl token create \
  -description "Lucid app services" \
  -policy-name lucid-app \
  -token "$CONSUL_HTTP_TOKEN" | tee consul-lucid-app-token.txt
```

把 `consul-lucid-app-token.txt` 中的 `SecretID` 写入 app `.env` 的 `CONSUL_TOKEN`。`consul-bootstrap-token.txt`、`consul-lucid-agent-token.txt` 和 `consul-lucid-app-token.txt` 只用于服务器初始化记录，不要提交到仓库。

## 5. 准备 App 环境变量

```bash
cp deploy/compose/app/.env.example deploy/compose/app/.env
```

至少修改以下值：

```env
ADMIN_ORIGIN=https://cloud.example.xyz
GATEWAY_PUBLIC_URL=https://api.example.xyz
LOCAL_ADMIN_ORIGIN=http://localhost:5173
LOCAL_ADMIN_ORIGIN_127=http://127.0.0.1:5173
IDENTITY_CONNECTION_STRING=Host=postgres;Port=5432;Database=lucid_micro_identity;Username=postgres;Password=与POSTGRES_PASSWORD一致
NOTIFICATION_CONNECTION_STRING=Host=postgres;Port=5432;Database=lucid_micro_notification;Username=postgres;Password=与POSTGRES_PASSWORD一致
REDIS_CONNECTION_STRING=redis:6379,password=与REDIS_PASSWORD一致,abortConnect=false
RABBITMQ_CONNECTION_STRING=amqp://admin:与RABBITMQ_DEFAULT_PASS一致@rabbitmq:5672/
CONSUL_ADDRESS=http://consul:8500
CONSUL_TOKEN=lucid-app token SecretID
JWT_SIGNING_KEY=至少32字节的生产签名密钥
```

RabbitMQ 用户名、密码或 vhost 如果包含特殊字符，写入 `amqp://` URI 时需要 URL encode。`GATEWAY_PUBLIC_URL` 会在 Vite 构建时写入 admin-web 静态资源；服务器域名或 Gateway 端口变化后，需要重新构建 `admin-web`。

使用 Caddy 时，`ADMIN_BIND` 和 `GATEWAY_BIND` 保持默认 `127.0.0.1`，不要把 `8088` 和 `49953` 直接暴露到公网。

`ADMIN_ORIGIN` 用于已发布前端访问 Gateway；`LOCAL_ADMIN_ORIGIN` 和 `LOCAL_ADMIN_ORIGIN_127` 用于本地 Vite 前端访问已发布 Gateway。CORS origin 必须精确匹配浏览器地址栏中的协议、域名和端口。

## 6. 执行数据库迁移

执行 Identity 和 Notification 数据库迁移，命令见 [数据库迁移](../development/database-migrations.md#执行迁移)。

执行完 Identity 迁移后，默认管理员为 `admin` / `Admin@123456`。生产环境首次登录后应立即修改默认密码。

## 7. 构建并启动 App

```bash
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --build
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml ps
```

## 8. 启动 Caddy

```bash
cp deploy/compose/caddy/.env.example deploy/compose/caddy/.env
```

编辑 `deploy/compose/caddy/.env`：

```env
CADDY_ACME_EMAIL=admin@example.xyz
CLOUDFLARE_API_TOKEN=Cloudflare API token
```

服务器防火墙或云安全组需要放行 TCP `443`。如果希望 HTTP 自动跳转 HTTPS，也放行 TCP `80`。

启动 Caddy：

```bash
docker compose --env-file deploy/compose/caddy/.env -f deploy/compose/caddy/docker-compose.yml up -d --build
docker compose --env-file deploy/compose/caddy/.env -f deploy/compose/caddy/docker-compose.yml ps
```

## 9. 验证

```bash
curl http://localhost:49953/health
curl http://localhost:8088/health
curl https://api.example.xyz/health
curl https://cloud.example.xyz/health
curl -X POST https://api.example.xyz/api/identity/admin-auth/login \
  -H "Content-Type: application/json" \
  -d '{"loginName":"admin","password":"Admin@123456"}'
```

外部浏览器访问 `admin-web`，业务 API 通过 `gateway` 进入。Identity 和 Notification 不应直接对公网暴露。
