# Gateway 设计

Gateway 是 LucidMicro 的统一 HTTP 入口，用于承接浏览器、移动端、第三方客户端到后端服务的流量。

它不是业务服务，也不承载领域逻辑。业务规则仍然留在各个服务自己的 Application / Domain 层。

## 目标

Gateway 需要解决这些问题：

- 统一外部入口，避免前端直接感知多个后端服务地址。
- 统一处理 CORS、基础请求日志、入口限流和常见安全头。
- 将外部路由映射到内部服务路由。
- 透传认证上下文、trace context 和必要的请求头。
- 为后续服务发现、灰度、熔断、超时和重试策略预留边界。

当前阶段已落地第一版 Gateway 运行时代码：

```text
backend/src/Gateway/LucidMicro.Gateway/
```

当前使用 YARP 完成反向代理，并已接入 Consul 动态服务发现。静态 YARP destination 仍作为禁用动态发现时的默认配置保留。

## 职责边界

Gateway 负责：

- 外部路由聚合，例如 `/api/identity/*` 转发到 Identity.Api。
- 浏览器入口 CORS。
- TLS 终止或反向代理入口配合。
- 统一 request logging。
- 统一限流、IP 策略和基础安全头。
- 向下游服务透传 `Authorization`、`traceparent`、`tracestate` 等必要头。

Gateway 不负责：

- 业务校验。
- 数据访问。
- 跨服务事务。
- 直接消费 MQ。
- 替代服务自身的 health checks、OpenAPI 和认证授权。

服务自身仍应保留：

- 独立 `/openapi/v1.json`。
- 独立 `/health/live` 和 `/health/ready`。
- 本地调试需要的 `Lucid:Cors`。
- 服务内部认证授权策略。

## 技术路线

LucidMicro 暂不锁死 Gateway 技术栈。

可选路线：

- Caddy：适合部署层反向代理、TLS、静态前端和简单路径转发。
- YARP：适合在 .NET 内实现可编程 Gateway、动态路由、服务发现和自定义管道。
- Ocelot：适合配置驱动的 .NET API Gateway，但扩展性和生态取舍需要单独评估。

默认演进建议：

1. 本地开发和早期部署使用 YARP Gateway。
2. 下游地址优先通过 Consul 动态发现，静态 destination 作为禁用动态发现时的默认配置。
3. 当需要统一鉴权策略、复杂流量治理或跨环境路由时，在当前 YARP Gateway 项目上继续演进。
4. 不在业务服务里实现 Gateway 逻辑。

## 路由约定

外部路由应带服务边界：

```text
/api/identity/*
/api/notification/*
```

下游服务可以继续保留自己的内部路由：

```text
Identity.Api       /api/admin-auth/*
Notification.Api   /api/notifications/*
```

Gateway 负责路径重写：

```text
/api/identity/admin-auth/login -> Identity.Api /api/admin-auth/login
/api/notification/notifications -> Notification.Api /api/notifications
```

路由形态：

```text
Gateway /api/identity/{**catch-all}
  -> Consul service identity
  -> Identity.Api /api/{**catch-all}

Gateway /api/notification/{**catch-all}
  -> Consul service notification
  -> Notification.Api /api/{**catch-all}
```

不要让前端直接依赖内部服务名、内部端口或部署拓扑。

## CORS 策略

引入 Gateway 后，浏览器入口 CORS 应优先收口到 Gateway。

业务服务中的 `Lucid:Cors` 仍然保留，但用途变成：

- 本地开发时前端直连某个服务。
- API contract 测试覆盖服务自身 CORS 能力。
- Gateway 尚未接入某个环境时的过渡方案。

生产环境中，如果所有浏览器流量都经过 Gateway，业务服务可以关闭：

```json
{
  "Lucid": {
    "Cors": {
      "Enabled": false
    }
  }
}
```

## 认证与上下文透传

短期策略：

- Gateway 不解析业务权限。
- Gateway 透传 `Authorization` 到下游服务。
- 下游服务继续用自己的 JWT auth BuildingBlock 做认证授权。

后续如果需要 Gateway 层鉴权，可以演进为：

- Gateway 只做 token 基础校验和黑白名单。
- 细粒度权限仍由下游服务判断。
- 用户身份、租户、trace context 必须通过标准 header 或 token claims 传递。

必须透传的观测性 header：

```text
traceparent
tracestate
```

可按需透传的业务上下文 header：

```text
X-Correlation-Id
X-Tenant-Id
```

这些 header 的正式启用应和认证、租户、链路追踪约定一起收口。

## OpenAPI 与 API Client

短期策略：

- 各服务继续独立暴露 `/openapi/v1.json`。
- 前端 API Client 仍从各服务 OpenAPI 生成。
- Gateway 不承担 OpenAPI 聚合。

后续如果前端只面向 Gateway，可以考虑增加 Gateway OpenAPI 聚合能力，但不要手写重复契约。

## 服务注册与发现

Gateway 已支持从 Consul 加载下游服务地址。配置中维护 Gateway cluster 到 Consul service name 的映射，例如：

```json
{
  "Lucid": {
    "Gateway": {
      "ServiceDiscovery": {
        "Enabled": true,
        "RefreshIntervalSeconds": 10,
        "LoadBalancingPolicy": "RoundRobin",
        "Clusters": {
          "identity": "identity",
          "notification": "notification"
        }
      }
    }
  }
}
```

Gateway 周期性读取 Consul 中 passing 的实例并更新 YARP cluster destinations。多个实例同时 passing 时，由 YARP 按配置的 load balancing policy 转发，当前 compose 使用 `RoundRobin`。

当前不要让业务服务主动依赖 Gateway。业务服务只负责暴露 HTTP、health checks 和 OpenAPI。

服务发现约定见 [服务发现约定](../conventions/service-discovery.md)。

## Health Checks

Gateway 自身需要 health checks：

```text
/health/live
/health/ready
```

`live` 只表示 Gateway 进程存活。

`ready` 可以按阶段演进：

- 第一版只检查 Gateway 配置可加载。
- 第二版检查关键下游服务地址可解析。
- 第三版根据需要探测下游 ready endpoint。

不要让 Gateway ready 强依赖所有业务服务都可用，否则一个边缘服务故障会让整个入口被摘除。

## 超时与重试

Gateway 可以统一设置基础超时，但默认不做激进重试。

建议：

- GET 查询可以按需允许短重试。
- POST / PUT / DELETE 默认不自动重试，除非接口明确幂等。
- 消息投递和最终一致性场景不通过 Gateway 重试解决。

## 当前结论

当前阶段已创建 Gateway：

- 业务服务继续独立运行。
- Gateway 使用 YARP 转发 Identity 和 Notification。
- Gateway 可以从 Consul 动态发现下游实例，并支持多实例负载均衡。
- 静态 destination 保留为动态发现关闭时的默认配置。
- CORS BuildingBlock 已为未来 Gateway 收口做好准备。
