# 短信登录本地 E2E 验证

状态：已落地，面向本机手动验证。

本文用于验证短信登录闭环，不依赖远程测试服务器，也不需要额外脚本。设计背景见 [短信登录技术设计](sms-login.md)。

## 验证目标

完整链路：

```text
Client
  -> Identity.Api POST /api/sms-login/codes
  -> Redis 保存验证码和限频 key
  -> Identity typed NotificationClient
  -> ServiceDiscovery.Consul 解析 notification
  -> Resilience.Http 包裹 HTTP 调用
  -> Notification.Api POST /internal/notifications
  -> notification_messages 保存验证码通知内容
  -> Client 取出验证码
  -> Identity.Api POST /api/sms-login
  -> Redis 校验并删除验证码
  -> AdminUser.PhoneNumber 查找管理员
  -> 签发 access token 和 refresh token
```

第一版还没有真实短信 provider。Notification 会创建 `Sms` 通知记录，并通过 `LogSmsNotificationChannelSender` 标记为已发送；本地验证时可以从通知内容里取出验证码。

## 前置依赖

- 本地 PostgreSQL 可访问。
- 本地 Redis 可访问。
- 本地 Consul 可访问。
- 本地 Notification.Api 可访问。
- Identity 数据库名：`lucid_micro_identity`。
- Notification 数据库名：`lucid_micro_notification`。
- Redis 默认连接：`localhost:6379`。
- Consul 默认地址：`http://localhost:8500`。
- Consul 中存在 passing 的 `notification` 服务实例，默认由 Notification 启动时自动注册。
- Notification 默认 HTTP 地址：`http://localhost:49853`。
- Identity 默认地址：`http://localhost:49753`。
- 默认管理员账号：`admin` / `Admin@123456`。
- 默认管理员手机号：`13800138000`。

默认配置位于：

- `backend/src/Services/Identity/LucidMicro.Services.Identity.Api/appsettings.json`
- `backend/src/Services/Notification/LucidMicro.Services.Notification.Api/appsettings.json`

如果本地连接信息不同，优先改 `appsettings.Development.json` 或使用环境变量覆盖。

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

- Identity 库包含 `admin_users` 表，并且默认管理员手机号为 `13800138000`。
- Identity 库包含 `admin_users.phone_number` 的唯一索引。
- Notification 库包含 `notification_messages` 表。

## 启动服务

建议先启动 Notification、Identity 和 Gateway：

```powershell
dotnet run --project backend\src\Services\Notification\LucidMicro.Services.Notification.Api\LucidMicro.Services.Notification.Api.csproj
dotnet run --project backend\src\Services\Identity\LucidMicro.Services.Identity.Api\LucidMicro.Services.Identity.Api.csproj
dotnet run --project backend\src\Gateway\LucidMicro.Gateway\LucidMicro.Gateway.csproj
```

启动后检查 ready：

```powershell
Invoke-RestMethod http://localhost:49853/ready
Invoke-RestMethod http://localhost:49753/ready
Invoke-RestMethod http://localhost:49953/health
```

期望：

- Notification ready 健康。
- Identity ready 健康。
- Gateway health 健康。
- 如果 Redis 不可用，Identity ready 会暴露 Redis 检查失败。
- 如果 Consul 不可用，Identity ready 会暴露 Consul 检查失败。

## 发送短信验证码

调用 Gateway 下的 Identity 发码接口：

```powershell
$phoneNumber = '13800138000'

Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:49953/api/identity/sms-login/codes `
  -ContentType 'application/json' `
  -Body (@{
    phoneNumber = $phoneNumber
  } | ConvertTo-Json)
```

期望：

- 返回 `204 No Content`。
- Redis 中出现短信登录相关 key。
- Notification 中出现一条 `Sms` 通知记录，`recipient` 为 `13800138000`。

Redis 可选检查：

```powershell
redis-cli keys "identity:sms-login:*:13800138000"
```

## 取出验证码

第一版没有真实短信 provider，因此本地验证可以从 Notification API 读取最近通知内容：

```powershell
$notifications = Invoke-RestMethod 'http://localhost:49953/api/notification/notifications?pageNumber=1&pageSize=10'
$sms = $notifications.items |
  Where-Object { $_.recipient -eq $phoneNumber -and $_.channel -eq 'Sms' } |
  Select-Object -First 1

$sms.content
$code = [regex]::Match($sms.content, '\d{6}').Value
$code
```

也可以直接查询 Notification 数据库：

```sql
select id, recipient, channel, subject, content, status, failed_at, failure_reason
from notification_messages
where recipient = '13800138000'
order by created_at desc
limit 1;
```

当前本地实现使用 `LogSmsNotificationChannelSender`，`status` 应为 `Sent`，`content` 会包含验证码，例如 `Your verification code is 123456.`。

## 使用验证码登录

调用 Gateway 下的短信登录接口：

```powershell
$login = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:49953/api/identity/sms-login `
  -ContentType 'application/json' `
  -Body (@{
    phoneNumber = $phoneNumber
    code = $code
  } | ConvertTo-Json)

$login.accessToken
$login.refreshToken
```

期望：

- 返回 access token、access token 过期时间、refresh token 和 refresh token 过期时间。
- Redis 中该手机号的验证码 key 被删除。
- `admin_users.last_login_at` 被更新。

Redis 可选检查：

```powershell
redis-cli keys "identity:sms-login:*:13800138000"
```

数据库可选检查：

```sql
select user_name, phone_number, last_login_at
from admin_users
where phone_number = '13800138000';
```

## 失败路径检查

建议至少手动扫一遍这些边界：

- 重复发码：60 秒内再次调用 `/api/identity/sms-login/codes`，应返回限频错误。
- 错误验证码：调用 `/api/identity/sms-login` 传入错误 code，应返回 `InvalidCode`。
- 多次错误验证码：达到 `MaxAttempts` 后验证码应被删除。
- 过期验证码：超过 `CodeTtlSeconds` 后登录应返回 `CodeExpired`。
- 未绑定管理员手机号：换一个没有 AdminUser 的手机号，即使验证码正确也应返回 `InvalidCredentials`。
- 禁用管理员：如果管理员被禁用，验证码正确也应返回 `Disabled`。

## 常见排查

- 发码接口返回 Notification unavailable：检查 Notification.Api 是否启动、Consul 是否可访问，以及 Consul 中是否存在 passing 的 `notification` 服务实例。
- 发码接口返回限频：等待 `SendIntervalSeconds` 后重试，或清理 Redis 中 `identity:sms-login:rate:{phone}`。
- Notification 查不到通知：检查 Identity 到 Notification 的 HTTP 调用日志。
- 登录返回 CodeExpired：确认验证码 key 还在 Redis 中，并且使用的是同一个手机号。
- 登录返回 InvalidCredentials：确认 `admin_users.phone_number` 等于请求手机号。
- 登录成功但下次同一验证码失败：这是预期行为，验证码成功使用后会立即删除。
