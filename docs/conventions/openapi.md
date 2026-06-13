# OpenAPI 约定

LucidMicro 使用 OpenAPI 作为 HTTP API 契约格式。

当前实现：

- `Microsoft.AspNetCore.OpenApi` 负责生成 OpenAPI 文档。
- `Scalar.AspNetCore` 负责展示 API reference UI。
- `LucidMicro.BuildingBlocks.OpenApi.AspNetCore` 封装统一注册入口。

不直接使用 Swagger/Swashbuckle 作为默认方案。Swagger UI 只是 OpenAPI 的一种展示方式，框架层默认以 OpenAPI 契约为中心。

## 端点

每个 API 服务默认暴露：

```text
/openapi/v1.json
/scalar
```

`/openapi/v1.json` 用于机器读取和后续客户端生成。

`/scalar` 用于开发者浏览接口。

API 路由版本化策略见 [API 约定](api.md)。

前端 API client 生成策略见 [API Client 生成策略](api-client.md)。

## 配置

配置统一放在 `Lucid:OpenApi`：

```json
{
  "Lucid": {
    "OpenApi": {
      "Title": "LucidMicro Identity API",
      "Version": "v1",
      "Description": "Identity service API.",
      "EnableBearerSecurity": true
    }
  }
}
```

`Title` 和 `Version` 是必填项。

默认启用 Bearer JWT security scheme。即使某些 endpoint 允许匿名访问，OpenAPI 文档仍会声明 Bearer scheme，方便 Scalar 中手动填入 access token 调试受保护接口。

## 服务接入

API 项目引用：

```text
LucidMicro.BuildingBlocks.OpenApi.AspNetCore
```

注册：

```csharp
builder.Services.AddLucidOpenApi(
    builder.Configuration.GetRequiredSection(LucidOpenApiOptions.ConfigurationSectionName));
```

映射：

```csharp
app.MapLucidOpenApi();
```

`MapLucidOpenApi` 应放在 `MapControllers` 之前或之后均可。当前服务统一放在 health checks 之后、controllers 之前。

## 当前服务

已接入：

- Identity.Api
- Notification.Api

后续新增服务时，默认接入 OpenAPI BuildingBlock，并在 API contract 测试中覆盖 `/openapi/v1.json`。
