# API 约定

本文档定义 LucidMicro HTTP API 的路由、版本化和兼容性策略。

## 当前版本化策略

第一阶段不引入显式 API versioning 包，也不在业务路由中写版本号。

当前路由保持简洁：

```text
/api/admin-auth/login
/api/admin-users
/api/notifications
```

OpenAPI 文档使用 `v1`：

```text
/openapi/v1.json
```

这里的 `v1` 表示当前 HTTP 契约文档版本，不代表所有路由都必须写成 `/api/v1/...`。

前端类型和 API client 生成策略见 [API Client 生成策略](api-client.md)。

## 为什么先不在 URL 写版本

当前服务主要面向内部管理端和服务间协作，API 数量还少，直接引入 URL 版本会带来额外样板：

- Controller 路由更重。
- 测试路径更重。
- OpenAPI 多文档配置更重。
- 还没有真实的多版本并存需求。

因此第一阶段使用“文档版本 + 兼容性约束”的轻量策略。

## 兼容性规则

默认不破坏已有请求和响应契约。

允许的兼容变更：

- 新增 endpoint。
- 新增可选请求字段。
- 新增响应字段。
- 新增 enum 值，但调用方必须能安全忽略未知值时才推荐。
- 放宽校验规则。

需要谨慎处理的破坏性变更：

- 删除 endpoint。
- 修改路由、HTTP method 或状态码语义。
- 删除响应字段。
- 重命名请求或响应字段。
- 把可选字段改为必填字段。
- 修改字段类型。
- 收紧校验规则。
- 改变分页、排序或过滤语义。

破坏性变更优先通过新增 endpoint 或新增字段完成。如果确实需要长期并存，再引入显式 API versioning。

## 什么时候引入显式版本

满足以下任一条件时，再考虑引入 URL 或 header 版本化：

- 存在外部客户或第三方系统直接调用 API。
- 同一个服务需要长期维护两套不兼容 HTTP 契约。
- 前端或移动端无法同步升级，需要老版本 API 保持可用。
- OpenAPI 需要同时发布多个文档，例如 `v1` 和 `v2`。

届时推荐策略：

```text
/api/v1/...
/api/v2/...
/openapi/v1.json
/openapi/v2.json
```

在此之前，不为“未来可能需要”提前引入版本化包。

## OpenAPI 文档版本

每个服务当前只发布一个 OpenAPI 文档：

```text
/openapi/v1.json
```

服务配置中的 `Lucid:OpenApi:Version` 应保持为 `v1`，除非引入第二个并行契约版本。

`v1` 可以包含向后兼容的新 endpoint 和新字段。只有当出现并行维护的不兼容契约时，才新增 `v2`。

## 路由命名

Controller 路由使用资源复数名：

```text
/api/admin-users
/api/notifications
```

认证、刷新 token 等动作型入口可以使用明确动作名：

```text
/api/admin-auth/login
/api/admin-auth/refresh
```

避免在路由中暴露内部实现细节，例如数据库表名、消息队列名称或 Provider 名称。

## 响应约定

成功响应保持具体资源模型或分页模型。

错误响应统一使用 BuildingBlock 的 ProblemDetails 映射，并包含 `traceId`，方便从 API 响应关联日志和链路追踪。

分页查询默认使用：

```text
pageNumber
pageSize
```

分页响应默认使用：

```text
items
totalCount
pageNumber
pageSize
```

后续新增服务应沿用该形态，除非有明确业务理由。
