# CORS 约定

CORS 是浏览器到 HTTP API 的入口边界策略，不是服务间通信能力。

LucidMicro 使用 `LucidMicro.BuildingBlocks.Cors.AspNetCore` 统一封装 ASP.NET Core CORS 注册和中间件顺序。

## 使用策略

默认策略：

- 本地开发或前端直接访问具体服务时，可以在对应 API 服务启用 CORS。
- 引入 Gateway 后，优先由 Gateway 统一处理 CORS，后端业务服务可以关闭 CORS。
- 服务间调用、消息消费、后台任务不依赖 CORS。

不要把 CORS 当成安全认证。认证仍由 JWT、Cookie 或网关认证策略负责。

## 配置

配置统一放在 `Lucid:Cors`：

```json
{
  "Lucid": {
    "Cors": {
      "Enabled": true,
      "AllowedOrigins": [
        "http://localhost:5173",
        "http://localhost:5174"
      ],
      "AllowedMethods": [
        "GET",
        "POST",
        "PUT",
        "DELETE",
        "OPTIONS"
      ],
      "AllowedHeaders": [
        "Authorization",
        "Content-Type"
      ],
      "AllowCredentials": false
    }
  }
}
```

`AllowedOrigins`、`AllowedMethods`、`AllowedHeaders` 支持 `"*"`。

当 `AllowCredentials` 为 `true` 时，`AllowedOrigins` 不能使用 `"*"`。这是浏览器 CORS 规范限制，也是 BuildingBlock 的启动校验规则。

## 服务接入

API 项目引用：

```text
LucidMicro.BuildingBlocks.Cors.AspNetCore
```

注册：

```csharp
builder.Services.AddLucidCors(
    builder.Configuration.GetRequiredSection(LucidCorsOptions.ConfigurationSectionName));
```

中间件：

```csharp
app.UseExceptionHandler();
app.UseLucidSerilogRequestLogging();
app.UseLucidCors();

app.UseAuthentication();
app.UseAuthorization();
```

`UseLucidCors` 应放在异常处理和 request logging 之后，认证授权之前。

## 当前服务

已接入：

- Identity.Api
- Notification.Api

当前本地前端默认允许：

- `http://localhost:5173`
- `http://localhost:5174`

当前浏览器入口已收敛到 Gateway。业务服务仍可在本地直连调试时启用 CORS；服务器部署时优先由 Gateway 统一处理 CORS，并按需关闭业务服务的 `Lucid:Cors:Enabled`。
