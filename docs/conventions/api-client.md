# API Client 生成策略

本文档定义前端接入后端 API 前的类型和调用代码生成策略。

## 目标

前端 API client 应以 OpenAPI 文档为来源，减少手写 DTO 和接口路径漂移。

当前后端每个服务都暴露：

```text
/openapi/v1.json
```

OpenAPI 和 API 版本策略见：

- [OpenAPI 约定](openapi.md)
- [API 约定](api.md)

## 第一阶段策略

第一阶段不生成完整请求函数，只生成 TypeScript 类型。

推荐工具：

```text
openapi-typescript
```

原因：

- 足够轻量。
- 只负责把 OpenAPI 转成 TypeScript 类型。
- 不绑定 axios、fetch、Vue Query 或其他请求库。
- 方便保留项目自己的 HTTP client、认证刷新和错误处理逻辑。

暂不默认使用：

- `orval`：能力完整，但会生成更多请求层代码，第一阶段偏重。
- `NSwag`：.NET 生态成熟，但前端项目里使用略重。
- `Kiota`：适合更正式的 SDK 场景，当前阶段不需要。

## 推荐目录

当前前端 workspace 已存在，API client 包尚未创建。落地时放在：

```text
frontend/packages/api-client/
  src/
    generated/
      identity.ts
      notification.ts
    http/
      httpClient.ts
      authTokenStore.ts
    services/
      identityClient.ts
      notificationClient.ts
```

`generated/` 只放生成产物，不手改。

`http/` 放项目自己的 HTTP 基础设施，例如：

- base URL 选择
- Authorization header
- refresh token
- ProblemDetails 解析
- traceId 展示或记录

`services/` 放轻量调用函数，使用生成类型约束入参和响应。

## 多服务组织

每个后端服务生成独立类型文件：

```text
identity.ts
notification.ts
```

调用函数也按服务拆分：

```text
identityClient
notificationClient
```

不要把所有服务接口放进一个巨大 client 文件。

## 命名约定

生成类型保留 OpenAPI 中的 schema 名称。

前端手写 client 使用 TypeScript 风格：

```ts
loginAdminUser()
getNotifications()
createNotification()
```

不要为了贴近 C# 而在前端使用 PascalCase 函数名。

## 认证与错误处理

HTTP client 统一处理：

- access token 注入
- refresh token 刷新
- 401 后的登录态失效
- ProblemDetails 错误结构
- `traceId`

业务页面不直接解析底层 HTTP 错误。

后端错误响应中的 `traceId` 应保留到前端错误对象中。用户报错或日志排查时，可以通过 `traceId` 关联后端日志和链路追踪。

## 生成时机

第一阶段可以手动执行生成命令。

等前端项目落地后，再把生成命令加入 package script，例如：

```json
{
  "scripts": {
    "generate:api": "openapi-typescript ..."
  }
}
```

OpenAPI 文档来源可以是：

- 本地运行服务的 `/openapi/v1.json`
- 后续 CI 导出的 OpenAPI 文件

在没有稳定前端 workspace 前，不急于添加生成脚本。

## 提交策略

第一阶段建议提交生成后的类型文件。

原因：

- 方便 IDE 类型提示。
- 避免每次 checkout 后必须先启动后端服务才能开发前端。
- CI 可以通过重新生成并比较 diff 来发现 API 契约漂移。

如果后续生成产物过大或变更噪音明显，再调整为 CI 生成但不提交。

## 演进方向

当接口数量明显增加，且手写 client 开始重复时，再考虑引入更完整的生成器。

候选方向：

- `orval`：生成请求函数，未来可接 Vue Query。
- `openapi-fetch`：和 `openapi-typescript` 配套，保持轻量类型安全调用。

在此之前，优先保持简单：OpenAPI 生成类型，项目自己维护薄薄一层 HTTP client。
