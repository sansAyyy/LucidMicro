# 数据库迁移

状态：已落地，适用于本地开发、单机部署和手动验证。

当前约定：数据库迁移由部署者或开发者显式执行，API 服务启动时不自动迁移数据库。这样可以避免多实例启动时竞争迁移，也能让生产发布过程更可控。

## 前置条件

- 已安装项目匹配的 .NET SDK，版本见 [global.json](../../global.json)。
- 已安装 `dotnet-ef`。
- PostgreSQL 可访问。
- Identity 数据库和 Notification 数据库已存在，或由 infra compose 首次初始化创建。

如果数据库还不存在，可以先创建空库：

```sql
create database lucid_micro_identity;
create database lucid_micro_notification;
```

使用 `deploy/compose/infra` 时，PostgreSQL volume 首次初始化会自动创建这两个业务数据库。

## 执行迁移

以下命令从仓库根目录执行。

PowerShell：

```powershell
dotnet ef database update `
  --project backend/src/Services/Identity/LucidMicro.Services.Identity.Infrastructure/LucidMicro.Services.Identity.Infrastructure.csproj `
  --startup-project backend/src/Services/Identity/LucidMicro.Services.Identity.Api/LucidMicro.Services.Identity.Api.csproj `
  --context IdentityDbContext

dotnet ef database update `
  --project backend/src/Services/Notification/LucidMicro.Services.Notification.Infrastructure/LucidMicro.Services.Notification.Infrastructure.csproj `
  --startup-project backend/src/Services/Notification/LucidMicro.Services.Notification.Api/LucidMicro.Services.Notification.Api.csproj `
  --context NotificationDbContext
```

Bash：

```bash
dotnet ef database update \
  --project backend/src/Services/Identity/LucidMicro.Services.Identity.Infrastructure/LucidMicro.Services.Identity.Infrastructure.csproj \
  --startup-project backend/src/Services/Identity/LucidMicro.Services.Identity.Api/LucidMicro.Services.Identity.Api.csproj \
  --context IdentityDbContext

dotnet ef database update \
  --project backend/src/Services/Notification/LucidMicro.Services.Notification.Infrastructure/LucidMicro.Services.Notification.Infrastructure.csproj \
  --startup-project backend/src/Services/Notification/LucidMicro.Services.Notification.Api/LucidMicro.Services.Notification.Api.csproj \
  --context NotificationDbContext
```

## 迁移结果

期望结果：

- Identity 库包含 `admin_users`、`outbox_messages` 等表。
- Notification 库包含 `notification_messages`、`inbox_messages` 等表。
- 执行完 Identity 迁移后会包含默认管理员：`admin` / `Admin@123456`，手机号为 `13800138000`。

生产环境首次登录后应立即修改默认密码。

## 常见问题

- 数据库不存在：先创建 `lucid_micro_identity` 和 `lucid_micro_notification`，或确认 infra compose 初始化完成。
- 连接失败：检查连接串、PostgreSQL 端口绑定、防火墙和容器网络。
- `dotnet ef` 不存在：安装或恢复本机 EF 工具。
- 默认管理员不存在：确认 Identity 迁移已执行到最新版本，并检查 migration 日志。

