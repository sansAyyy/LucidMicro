# GitHub Actions 部署

状态：第一版，面向单机 Docker Compose 部署。

本文档说明如何通过 GitHub Actions 构建 LucidMicro 应用镜像、推送到 GHCR，并通过 SSH 更新服务器上的 app compose。

## 部署方式

发布流水线位于：

```text
.github/workflows/deploy.yml
```

默认触发方式：

- 推送到 `main` 分支。
- 在 GitHub Actions 页面手动执行 `workflow_dispatch`。

流水线会执行：

1. 运行后端测试。
2. 构建 Admin 前端。
3. 构建并推送 `identity-api`、`notification-api`、`gateway`、`admin-web` 镜像到 GHCR。
4. SSH 登录服务器，更新部署目录，执行 app compose 的 `pull` 和 `up -d`。

## 镜像

默认镜像命名为：

```text
ghcr.io/<owner>/lucidmicro/identity-api
ghcr.io/<owner>/lucidmicro/notification-api
ghcr.io/<owner>/lucidmicro/gateway
ghcr.io/<owner>/lucidmicro/admin-web
```

每次构建会推送两个 tag：

- 当前 commit sha。
- `main`。

服务器第一版建议使用 `main` tag，更新最简单。需要更强可追溯性时，可以把服务器 `.env` 中的镜像 tag 固定为某个 commit sha 后再执行 compose 更新。

## GitHub Secrets

在 GitHub 仓库的 `Settings -> Secrets and variables -> Actions` 中配置：

```text
DEPLOY_HOST
DEPLOY_USER
DEPLOY_SSH_KEY
DEPLOY_PATH
GATEWAY_PUBLIC_URL
```

含义：

```text
DEPLOY_HOST        服务器 IP 或域名
DEPLOY_USER        SSH 用户
DEPLOY_SSH_KEY     可登录服务器的私钥
DEPLOY_PATH        服务器上的 LucidMicro 部署目录，例如 /opt/lucidmicro
GATEWAY_PUBLIC_URL 浏览器访问 Gateway 的公开地址，例如 https://api.example.xyz
```

如果 GHCR package 是私有的，服务器拉镜像还需要：

```text
GHCR_USERNAME
GHCR_TOKEN
```

`GHCR_TOKEN` 使用 fine-grained personal access token 或 classic token 均可，至少需要读取 package 的权限。

## 服务器准备

服务器需要安装：

```text
docker
docker compose
git
```

部署目录示例：

```bash
sudo mkdir -p /opt/lucidmicro
sudo chown "$USER":"$USER" /opt/lucidmicro
git clone https://github.com/<owner>/<repo>.git /opt/lucidmicro
cd /opt/lucidmicro
```

首次部署前，先按 Docker Compose 文档完成 infra、Consul ACL、app `.env` 和 Caddy 配置：

```text
docs/deployment/docker-compose-quickstart.md
docs/deployment/docker-compose-operations.md
```

## App Compose 镜像配置

`deploy/compose/app/docker-compose.yml` 支持通过环境变量指定镜像。服务器的 `deploy/compose/app/.env` 中建议配置：

```env
IDENTITY_API_IMAGE=ghcr.io/<owner>/lucidmicro/identity-api:main
NOTIFICATION_API_IMAGE=ghcr.io/<owner>/lucidmicro/notification-api:main
GATEWAY_IMAGE=ghcr.io/<owner>/lucidmicro/gateway:main
ADMIN_WEB_IMAGE=ghcr.io/<owner>/lucidmicro/admin-web:main
```

其余数据库、Redis、RabbitMQ、Consul、JWT、端口和域名变量继续按 app compose 文档配置。

如果 GHCR package 是私有的，先在服务器登录一次：

```bash
echo "<token>" | docker login ghcr.io -u "<username>" --password-stdin
```

Actions 部署脚本也会在配置了 `GHCR_USERNAME` 和 `GHCR_TOKEN` 时自动登录。

部署脚本会在 `DEPLOY_PATH` 是 Git 仓库时执行：

```bash
git pull --ff-only
```

如果服务器上修改了已被 Git 跟踪的文件，`git pull --ff-only` 会失败。生产配置应放在 `.env` 这类不提交的文件中，避免和仓库文件冲突。

## 手动更新

服务器上可手动执行：

```bash
cd /opt/lucidmicro
git pull
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml pull
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml up -d --no-build --remove-orphans
```

查看状态：

```bash
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml ps
docker compose --env-file deploy/compose/app/.env -f deploy/compose/app/docker-compose.yml logs -f
```

## 数据库迁移

当前流水线不自动执行数据库迁移。

发布涉及数据库 schema 变更时，先按迁移文档显式执行迁移，再更新 app compose。后续可以增加一次性 migrator 镜像和 compose profile，把迁移纳入发布流程。

## 注意事项

- `GATEWAY_PUBLIC_URL` 会在 Admin 前端构建时写入静态资源，修改后必须重新构建并发布 `admin-web`。
- Caddy、infra compose 和生产 `.env` 不由 Actions 自动修改。
- 服务器上的 app `.env` 包含生产密钥，不要提交到 Git。
- 使用 `main` tag 时，服务器每次 `pull` 都会更新到最新 main 镜像；需要回滚时可把 `.env` 中镜像 tag 改成历史 commit sha 后重新 `up -d`。
