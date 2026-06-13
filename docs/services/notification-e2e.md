# Notification 本地 E2E 验证

状态：已落地，面向本机手动验证。

本文用于验证 Identity -> Outbox -> RabbitMQ -> Notification -> Inbox 链路，不依赖远程测试服务器，也不需要额外脚本。设计背景见 [Notification 服务设计](notification.md)。

## 验证目标

完整链路：

```text
Identity create admin user
  -> identity.outbox_messages
  -> Identity Outbox publisher
  -> RabbitMQ lucid.events
  -> Notification consumer
  -> notification.notification_messages
  -> notification.inbox_messages
```

## 前置依赖

- 本地 PostgreSQL 可访问。
- 本地 RabbitMQ 可访问。
- Identity 数据库名：`lucid_micro_identity`。
- Notification 数据库名：`lucid_micro_notification`。
- RabbitMQ 默认连接：`amqp://guest:guest@localhost:5672/`。
- Identity seed migration 会写入一个本地验证用管理员账号。

默认配置位于：

- `backend/src/Services/Identity/LucidMicro.Services.Identity.Api/appsettings.json`
- `backend/src/Services/Notification/LucidMicro.Services.Notification.Api/appsettings.json`

如果本地 PostgreSQL / RabbitMQ 连接信息不同，优先改 `appsettings.Development.json` 或使用环境变量覆盖，不建议直接改提交用的默认配置。

## 创建本地数据库

如果数据库还不存在，可以先创建空库：

```sql
create database lucid_micro_identity;
create database lucid_micro_notification;
```

如果库已存在，跳过这一步。

## 应用数据库迁移

在仓库根目录执行 Identity 和 Notification 数据库迁移，命令见 [数据库迁移](../development/database-migrations.md#执行迁移)。

期望结果：

- Identity 库包含 `admin_users`、`outbox_messages` 等表。
- Notification 库包含 `notification_messages`、`inbox_messages` 等表。
- 执行完 Identity 迁移后会包含一个默认管理员：`admin` / `Admin@123456`，手机号为 `13800138000`。

## 启动服务

建议先启动 Notification、Identity 和 Gateway：

```powershell
dotnet run --project backend\src\Services\Notification\LucidMicro.Services.Notification.Api\LucidMicro.Services.Notification.Api.csproj
dotnet run --project backend\src\Services\Identity\LucidMicro.Services.Identity.Api\LucidMicro.Services.Identity.Api.csproj
dotnet run --project backend\src\Gateway\LucidMicro.Gateway\LucidMicro.Gateway.csproj
```

默认 HTTP 地址：

- Identity HTTP：`http://localhost:49753`
- Notification HTTP：`http://localhost:49853`
- Gateway HTTP：`http://localhost:49953`

启动后检查 ready：

```powershell
Invoke-RestMethod http://localhost:49753/ready
Invoke-RestMethod http://localhost:49853/ready
Invoke-RestMethod http://localhost:49953/health
```

期望：

- 两个 ready endpoint 返回健康状态。
- Gateway health 返回健康状态。
- 如果 RabbitMQ 不可用，ready 会暴露 RabbitMQ 检查失败。

## 触发完整链路

完整链路通过 Identity 创建管理员用户触发。

步骤：

1. 使用已有管理员调用 Identity 登录接口，获取 access token。
2. 使用 token 调用 `POST /api/identity/admin-users`。
3. Identity 写入 `outbox_messages`。
4. Identity Outbox publisher 发布到 RabbitMQ。
5. Notification consumer 消费事件并创建通知记录。

登录示例：

```powershell
$login = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:49953/api/identity/admin-auth/login `
  -ContentType 'application/json' `
  -Body '{
    "loginName": "admin",
    "password": "Admin@123456"
  }'

$token = $login.accessToken
```

创建管理员用户：

```powershell
$suffix = Get-Date -Format 'yyyyMMddHHmmss'
$userName = "demo-admin-$suffix"
$email = "demo-admin-$suffix@example.com"
$body = @{
  userName = $userName
  email = $email
  displayName = 'Demo Admin'
  password = 'P@ssw0rd123'
  isActive = $true
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:49953/api/identity/admin-users `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType 'application/json' `
  -Body $body

Write-Host "Expected notification recipient: $email"
```

## 验证 Identity Outbox

在 Identity 数据库执行：

```sql
select id, type, published_at, failure_count, last_error
from outbox_messages
order by created_at desc
limit 5;
```

期望：

- `type` 为 `notification.send-requested.v1`。
- RabbitMQ 可用时，`published_at` 最终有值。
- 如果 `published_at` 为空，查看 Identity 日志中的 Outbox publisher 错误。

## 验证 Notification 消费结果

可以先通过 Notification API 查看最近通知：

```powershell
Invoke-RestMethod 'http://localhost:49953/api/notification/notifications?pageNumber=1&pageSize=10'
```

也可以按通知 id 查询详情：

```powershell
Invoke-RestMethod http://localhost:49953/api/notification/notifications/<notification-id>
```

需要直接看数据库时，可以执行：

```sql
select id, recipient, channel, subject, status, sent_at, failed_at, failure_reason
from notification_messages
order by created_at desc
limit 5;

select id, type, processed_at
from inbox_messages
order by processed_at desc
limit 5;
```

期望：

- `notification_messages.recipient` 为创建管理员用户时的 email。
- `channel` 为 `InApp`。
- `status` 第一版通常为 `Sent`，因为当前使用 log channel sender。
- `inbox_messages.id` 等于对应 integration event id。

## 可选：只验证 Notification API

如果暂时不想启动 Identity 或 RabbitMQ，可以先跳过完整 MQ 链路，只验证 Notification 服务本身：

```powershell
$body = @{
  recipient = 'local@example.com'
  channel = 'InApp'
  subject = 'Local notification test'
  content = 'Created through Notification API.'
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:49853/internal/notifications `
  -ContentType 'application/json' `
  -Body $body
```

该方式只验证 Notification API、数据库和 log channel sender，不验证 Identity Outbox、RabbitMQ 和 Inbox consumer。

## 常见排查

- `/ready` 不健康：先检查 PostgreSQL 和 RabbitMQ 是否可访问。
- Identity 登录失败：确认数据库迁移已执行，并使用默认账号 `admin` / `Admin@123456`。
- Identity `outbox_messages.published_at` 一直为空：检查 RabbitMQ 连接串和 Identity Outbox publisher 日志。
- Notification 没有创建记录：检查 Notification consumer 是否启动，RabbitMQ exchange/queue/binding 是否存在。
- 重复投递：检查 `inbox_messages` 是否已有相同 event id。
