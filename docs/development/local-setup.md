# 本地开发快速开始

状态：已落地，面向本机开发和功能联调。

本文只保留最短路径。完整部署说明见 [Docker Compose 部署](../deployment/docker-compose.md)。

## 前置依赖

- .NET SDK，版本以 [global.json](../../global.json) 为准。
- Docker 和 Docker Compose plugin。
- Node.js、Corepack、pnpm。
- PostgreSQL、Redis、RabbitMQ、Consul 可以用 `deploy/compose/infra` 启动。

## 1. 启动基础设施

```powershell
docker network inspect lucid-app *> $null
if ($LASTEXITCODE -ne 0) { docker network create lucid-app }

Copy-Item deploy/compose/infra/.env.example deploy/compose/infra/.env
docker compose --env-file deploy/compose/infra/.env -f deploy/compose/infra/docker-compose.yml up -d
```

Consul 启用 ACL 后需要初始化 token。完整步骤见 [Docker Compose 快速部署](../deployment/docker-compose-quickstart.md#4-初始化-consul-acl)。

## 2. 执行数据库迁移

执行 Identity 和 Notification 数据库迁移，命令见 [数据库迁移](database-migrations.md#执行迁移)。

迁移完成后默认管理员为：

```text
admin / Admin@123456
```

## 3. 启动后端服务

分别打开终端启动：

```powershell
dotnet run --project backend/src/Services/Notification/LucidMicro.Services.Notification.Api/LucidMicro.Services.Notification.Api.csproj
dotnet run --project backend/src/Services/Identity/LucidMicro.Services.Identity.Api/LucidMicro.Services.Identity.Api.csproj
dotnet run --project backend/src/Gateway/LucidMicro.Gateway/LucidMicro.Gateway.csproj
```

默认入口：

```text
Identity:      http://localhost:49753
Notification:  http://localhost:49853
Gateway:       http://localhost:49953
```

前端和浏览器联调时优先访问 Gateway：

```text
/api/identity/*
/api/notification/*
```

## 4. 启动 Admin

```powershell
Copy-Item frontend/apps/admin/.env.example frontend/apps/admin/.env
cd frontend
corepack pnpm install
corepack pnpm dev:admin
```

Admin 默认使用：

```env
VITE_API_BASE_URL=http://localhost:49953
```

## 5. 常用检查

```powershell
Invoke-RestMethod http://localhost:49953/health
```

登录：

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:49953/api/identity/admin-auth/login `
  -ContentType 'application/json' `
  -Body '{"loginName":"admin","password":"Admin@123456"}'
```

## 常用命令

后端测试：

```powershell
dotnet test backend/LucidMicro.slnx
```

Admin 构建：

```powershell
cd frontend
corepack pnpm build:admin
```

生成 CRUD 模块：

```powershell
.\scripts\new-crud.ps1
```
