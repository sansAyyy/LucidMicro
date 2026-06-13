# Docker Compose 运维操作

状态：已落地，适合日常启动、更新、排查和联调。

以下命令从仓库根目录执行。详细环境变量和服务说明见 [Docker Compose 参考](docker-compose-reference.md)。

## Infra 操作

创建共享网络：

```bash
docker network inspect lucid-app >/dev/null 2>&1 || docker network create lucid-app
```

复制环境变量：

```bash
cp deploy/compose/infra/.env.example deploy/compose/infra/.env
```

启动或更新 infra：

```bash
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml up -d
```

查看状态和 Consul 日志：

```bash
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml ps
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml logs -f consul
```

只重建 Consul 端口绑定：

```bash
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml up -d --force-recreate consul
```

不要为了重建端口绑定执行 `down -v`，否则会删除 infra volumes，PostgreSQL、Redis、RabbitMQ、Consul 和 Grafana 的持久化数据都会被清空。

## App 操作

复制环境变量：

```powershell
Copy-Item deploy/compose/app/.env.example deploy/compose/app/.env
```

构建全部服务：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml build
```

启动或更新全部服务：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --build
```

单独构建并更新某个服务：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml build notification-api
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --no-deps notification-api
```

使用已有镜像启动，不重新构建：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --no-build gateway
```

查看状态和日志：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml ps
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml logs -f
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml logs -f identity-api
```

重启、停止或移除单个服务：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml restart gateway
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml stop gateway
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml rm gateway
```

停止应用服务：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml down
```

应用 compose 不负责数据库迁移。启动 `up -d` 前，必须已经手动完成数据库创建和迁移。

## Caddy 操作

复制环境变量：

```powershell
Copy-Item deploy/compose/caddy/.env.example deploy/compose/caddy/.env
```

启动或更新 Caddy：

```powershell
docker compose --env-file deploy/compose/caddy/.env -f deploy/compose/caddy/docker-compose.yml up -d --build
```

查看状态和日志：

```powershell
docker compose --env-file deploy/compose/caddy/.env -f deploy/compose/caddy/docker-compose.yml ps
docker compose --env-file deploy/compose/caddy/.env -f deploy/compose/caddy/docker-compose.yml logs -f caddy
```

停止 Caddy：

```powershell
docker compose --env-file deploy/compose/caddy/.env -f deploy/compose/caddy/docker-compose.yml down
```

Caddy 证书和运行数据保存在 `caddy_data` volume 中。不要用 `down -v`，否则会删除证书缓存并触发重新申请。

如果只修改 `CLOUDFLARE_API_TOKEN` 或 `CADDY_ACME_EMAIL`，重新执行 `up -d` 即可。如果修改 `deploy/docker/caddy/Dockerfile` 或插件版本，执行 `up -d --build`。

## 多实例验证

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --no-build --scale notification-api=2 --scale identity-api=2
```

缩回单实例：

```powershell
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --no-build --scale notification-api=1 --scale identity-api=1
```

Gateway 会通过 Consul 读取 passing 实例并更新 YARP cluster destination。静态 destination 仍作为禁用动态发现时的默认地址。

## 远程访问管理端口

infra compose 默认把 PostgreSQL、Redis、RabbitMQ、Consul、Grafana、Loki、Tempo、Prometheus 和 OpenTelemetry Collector 的宿主机端口绑定到 `127.0.0.1`。本地开发机需要临时访问服务器上的管理端口时，优先使用 SSH tunnel：

```bash
ssh \
  -L 5432:127.0.0.1:5432 \
  -L 6379:127.0.0.1:6379 \
  -L 15672:127.0.0.1:15672 \
  -L 3000:127.0.0.1:3000 \
  -L 8500:127.0.0.1:8500 \
  user@server
```

然后在本机访问：

```text
PostgreSQL: 127.0.0.1:5432
Redis:      127.0.0.1:6379
RabbitMQ UI:http://127.0.0.1:15672
Grafana:    http://127.0.0.1:3000
Consul UI:  http://127.0.0.1:8500
```

直接暴露管理端口只建议用于开发环境或受控内网。公网开放时应至少用服务器防火墙或云安全组限制来源 IP。

## 验证命令

```powershell
Invoke-RestMethod http://localhost:49953/health
Invoke-RestMethod http://localhost:8088/health
Invoke-RestMethod https://api.example.xyz/health
Invoke-RestMethod https://cloud.example.xyz/health
```

登录：

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri https://api.example.xyz/api/identity/admin-auth/login `
  -ContentType 'application/json' `
  -Body '{"loginName":"admin","password":"Admin@123456"}'
```

短信发码：

```powershell
Invoke-WebRequest `
  -Method Post `
  -Uri http://localhost:49953/api/identity/sms-login/codes `
  -ContentType 'application/json' `
  -Body '{"phoneNumber":"13800138000"}'
```

通知列表：

```powershell
Invoke-RestMethod http://localhost:49953/api/notification/notifications?pageNumber=1&pageSize=10
```

