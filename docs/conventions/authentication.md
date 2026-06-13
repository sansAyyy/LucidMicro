# 认证配置与接口约定

本文档记录当前后端认证配置和 Identity 管理员认证接口约定。

## JWT 配置

JWT 配置节固定为 `Authentication:Jwt`。

```json
{
  "Authentication": {
    "Jwt": {
      "Issuer": "LucidMicro.Identity",
      "Audience": "LucidMicro.Admin",
      "RefreshAudience": "LucidMicro.Admin.Refresh",
      "SigningKey": "change-me-to-a-secure-32-byte-minimum-signing-key",
      "ExpirationMinutes": "60",
      "RefreshExpirationMinutes": "10080"
    }
  }
}
```

字段说明：

- `Issuer`：JWT 签发方。
- `Audience`：access token 受众。
- `RefreshAudience`：refresh token 受众，应与 `Audience` 不同，避免 refresh token 被普通 Bearer 认证接受；未配置时默认使用 `{Audience}.Refresh`。
- `SigningKey`：对称签名密钥，至少 32 字节。
- `ExpirationMinutes`：access token 过期分钟数。
- `RefreshExpirationMinutes`：refresh token 过期分钟数。

## 管理员登录

```http
POST /api/admin-auth/login
```

请求：

```json
{
  "loginName": "admin",
  "password": "secret"
}
```

响应：

```json
{
  "accessToken": "...",
  "expiresAt": "2026-05-24T13:00:00+00:00",
  "refreshToken": "...",
  "refreshTokenExpiresAt": "2026-05-31T12:00:00+00:00"
}
```

`loginName` 支持用户名或邮箱。

## 刷新 Token

```http
POST /api/admin-auth/refresh
```

请求：

```json
{
  "refreshToken": "..."
}
```

响应与登录接口一致，会返回新的 access token 和 refresh token。

当前 refresh token 是无状态 JWT。服务端会校验签名、签发方、过期时间和 `RefreshAudience`，并检查管理员用户是否存在且启用。

当前版本暂不支持：

- 服务端撤销 refresh token。
- refresh token 轮换后废弃旧 token。
- 设备会话管理。
- 用户改密后自动撤销历史 refresh token。
