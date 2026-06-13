# 服务发现约定

服务发现用于解决服务间调用和 Gateway 转发时“目标服务在哪里”的问题。

它只负责发现目标地址，不负责调用失败处理，也不负责业务路由规则。

## 职责边界

服务发现负责：

- 根据服务名解析一个或多个可用地址。
- 为 Gateway 或 typed `HttpClient` 提供下游地址。
- 隐藏本地端口、容器 DNS、Kubernetes Service 或注册中心细节。

服务发现不负责：

- 超时、重试、熔断，这些属于 Resilience。
- 鉴权、租户、业务校验。
- RabbitMQ consumer 分发。
- 数据库、Redis 等基础设施连接发现。

概念关系：

```text
Service Discovery  找到目标服务地址
Load Balancing     多个实例中选一个
Resilience         调用失败时控制超时、重试、熔断
```

## 使用场景

推荐用于：

- Gateway 转发到 Identity、Notification 等业务服务。
- 服务间 HTTP 调用，例如 Identity 调 Notification。
- 本地、容器、Kubernetes 或注册中心环境下统一地址解析。

不推荐用于：

- 业务层直接按服务名拼 URL。
- 替代 Gateway 的外部路由。
- 替代 health checks。

## 第一版方向

第一版已落地静态配置型服务发现和 Consul provider，不急着接 Nacos、etcd 或 Kubernetes API。

原因：

- 当前主要目标是验证框架边界。
- 本地开发、单机联调和部署环境优先使用同一套 Consul 发现语义。
- 先把调用方从硬编码 URL 中解耦出来，再保持 provider 替换边界清晰。

推荐配置：

```json
{
  "Lucid": {
    "ServiceDiscovery": {
      "Consul": {
        "Address": "http://localhost:8500",
        "OnlyPassing": true,
        "CacheDurationSeconds": 10,
        "RequestTimeoutSeconds": 5
      }
    }
  }
}
```

## BuildingBlock 拆分

当前已拆成：

```text
BuildingBlocks/Communication/ServiceDiscovery/
  LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions/
  LucidMicro.BuildingBlocks.ServiceDiscovery.Consul/
  LucidMicro.BuildingBlocks.ServiceDiscovery.Http/
  LucidMicro.BuildingBlocks.ServiceDiscovery.Static/
```

后续按需要再新增：

```text
LucidMicro.BuildingBlocks.ServiceDiscovery.Kubernetes
```

业务服务只依赖 `ServiceDiscovery.Abstractions` 和 `ServiceDiscovery.Http`，具体 provider 由启动层选择。

## 抽象形态

当前最小抽象：

```csharp
public interface IServiceEndpointResolver
{
    ValueTask<IReadOnlyList<Uri>> ResolveAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}
```

Resolver 只返回地址列表，具体来源由 Static、Consul 或后续 provider 决定。

当前已提供最小 endpoint selector：

```csharp
public interface IServiceEndpointSelector
{
    Uri Select(string serviceName, IReadOnlyList<Uri> endpoints);
}
```

`ServiceDiscovery.Abstractions` 提供默认 round-robin selector，Static 和 Consul 可以复用同一套 endpoint 选择逻辑。

不要在第一版把发现、负载均衡、健康探测和 resilience 全塞进一个接口。

## 与 HttpClient 的组合

服务间 HTTP 调用推荐使用 typed `HttpClient`。

服务间 HTTP 调用的调用方分层、契约放置和错误边界见 [服务间 HTTP 调用约定](service-to-service-http.md)。

早期静态地址写法：

```csharp
builder.Services
    .AddHttpClient<INotificationClient, NotificationClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:49853");
    })
    .AddLucidStandardHttpResilienceHandler(
        builder.Configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
```

当前推荐通过服务发现解析目标地址：

```csharp
builder.Services
    .AddHttpClient<INotificationClient, NotificationClient>()
    .AddLucidServiceDiscovery("notification")
    .AddLucidStandardHttpResilienceHandler(
        builder.Configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
```

`ServiceDiscovery.Http` 提供 `AddLucidServiceDiscovery(...)`，它只依赖 `IServiceEndpointResolver` 和 `IServiceEndpointSelector`。因此 Static、Consul 或其他 provider 后续都可以复用同一个 HTTP 接入层。

`AddLucidServiceDiscovery(...)` 会给 `HttpClient` 设置一个服务名占位 `BaseAddress`，因此 typed client 可以继续使用相对路径。handler 发现请求目标是该服务名占位地址时，会解析真实 endpoint 并替换 URI。

调用方显式传入其他绝对 URI 时，handler 会尊重该 URI，不做服务发现改写。

调用顺序上：

```text
typed HttpClient
  -> 服务发现 / 地址选择
  -> Resilience handler
  -> HTTP transport
```

当前约定是先解析服务地址，再交给 Resilience handler 处理调用失败边界。后续如果要让 retry 跨实例重选 endpoint，需要单独验证和设计。

## 与 Gateway 的关系

Gateway 也会使用服务发现，但 Gateway 不应成为业务服务之间调用的硬依赖。

推荐：

- 外部流量通过 Gateway。
- 服务间 HTTP 调用可以直接使用服务发现。
- Gateway 的下游地址也从服务发现获取。

Gateway 负责外部路由和路径重写，服务发现负责把服务名解析成内部地址。

## Health Checks

服务发现不替代 health checks。

服务发现只表达“有哪些可调用地址”。目标服务是否健康由 provider 或 health check 决定。

后续可以演进：

- Gateway ready 检查关键服务名是否能解析到地址。
- 独立健康探测组件定期过滤不可用 endpoint。
- 服务发现 provider 读取注册中心已有健康状态。

当前不要让服务发现注册入口主动探测所有下游服务，否则启动会被下游服务可用性强绑定。

## 与短信登录的关系

短信登录通过 typed `HttpClient` 调 Notification，地址来源由 Consul 服务发现提供。

Identity 切换地址来源时，不改变：

- 短信验证码逻辑。
- Redis key 约定。
- Notification API 契约。
- Resilience 策略。

## Consul 设计

`ServiceDiscovery.Consul` 已作为独立 provider 接入，不改变 `ServiceDiscovery.Abstractions` 和 `ServiceDiscovery.Http`。

当前 Consul provider 同时提供发现入口和自注册入口，但二者保持分离：

```csharp
services.AddLucidConsulServiceDiscovery(...);
services.AddLucidConsulServiceRegistration(...);
```

发现能力负责解析服务名。注册能力负责应用启动时向 Consul Agent 注册当前服务实例，并在停止时注销。

推荐配置：

```json
{
  "Lucid": {
    "ServiceDiscovery": {
      "Consul": {
        "Address": "http://localhost:8500",
        "Datacenter": "",
        "Token": "",
        "OnlyPassing": true,
        "CacheDurationSeconds": 10,
        "RequestTimeoutSeconds": 5,
        "Registration": {
          "ServiceName": "notification",
          "UseInstanceDefaults": true,
          "Port": 49853,
          "Scheme": "http",
          "HealthCheckPath": "/ready",
          "HealthCheckIntervalSeconds": 10,
          "DeregisterCriticalServiceAfterSeconds": 60
        }
      }
    }
  }
}
```

字段说明：

- `Address`：Consul HTTP API 地址。
- `Datacenter`：可选 datacenter。为空时使用 Consul 默认 datacenter。
- `Token`：可选 ACL token。不要提交真实 token。
- `OnlyPassing`：是否只返回 health check passing 的实例，默认 `true`。
- `CacheDurationSeconds`：发现结果本地缓存时间，避免每次 HTTP 调用都请求 Consul。
- `RequestTimeoutSeconds`：调用 Consul HTTP API 的单次超时时间。
- `Registration`：当前服务实例注册信息。只在需要该服务自注册时配置。

注册字段说明：

- `ServiceName`：Consul 服务名，使用小写短名，例如 `identity`、`notification`。
- `UseInstanceDefaults`：是否使用当前运行实例的默认信息。Docker Compose 多实例部署建议开启。
- `ServiceId`：当前实例 id。单机本地可使用 `notification-local`，多实例部署必须保证唯一。
- `Address`：当前实例对其他服务可访问的地址，不包含 scheme 和端口。
- `Port`：当前实例 HTTP 端口。
- `Scheme`：当前实例 HTTP scheme，支持 `http` 或 `https`。
- `HealthCheckPath`：Consul HTTP check 路径，默认使用 `/ready`。
- `HealthCheckIntervalSeconds`：Consul check 间隔。
- `DeregisterCriticalServiceAfterSeconds`：实例 critical 后自动注销等待时间。

开启 `UseInstanceDefaults` 后，注册组件会使用：

- `ServiceId = {ServiceName}-{Environment.MachineName}`。
- `Address = 当前实例可解析到的非 loopback IP`。

这种模式适合 Docker Compose scale 场景，避免多个容器实例使用同一个固定 `ServiceId` 或固定服务名地址注册。关闭 `UseInstanceDefaults` 时，必须显式配置 `ServiceId` 和 `Address`。

### 查询策略

第一版建议使用 Consul Health API，而不是 Catalog API。

原因：

- Catalog API 只表达服务注册信息，不一定表达健康状态。
- Health API 可以按服务名返回实例及健康检查结果。
- `OnlyPassing = true` 时，调用方天然避开不健康实例。
- 当前只需要服务发现这一条窄路径，直接使用 Consul HTTP API 可以减少 provider 依赖，并保持测试简单。

当前不引入 Consul.NET 客户端包。该包本质上也是对 Consul HTTP API 的 .NET 封装；第一版只使用健康服务查询和基础服务注册，直接使用 `HttpClient` 更克制。后续如果需要 blocking query、watch、TTL check 或更完整的 Consul agent 能力，再重新评估是否引入客户端包。

推荐查询语义：

```text
GET /v1/health/service/{serviceName}?passing=true
```

如果配置了 datacenter：

```text
GET /v1/health/service/{serviceName}?passing=true&dc={datacenter}
```

### Endpoint 生成

Consul 返回实例后，provider 应生成 absolute HTTP/HTTPS URI。

推荐优先级：

1. 如果 service meta 中显式配置 `scheme`，使用该 scheme。
2. 否则默认使用 `http`。
3. host 优先使用 `Service.Address`。
4. 如果 `Service.Address` 为空，使用 `Node.Address`。
5. port 使用 `Service.Port`。

第一版不处理 path prefix。服务发现只返回服务根地址，例如：

```text
http://10.0.0.12:49853
```

具体 API path 仍由 typed client 请求路径决定。

### 缓存策略

第一版 Consul provider 应缓存发现结果。

建议：

- 按 `serviceName` 缓存 endpoint 列表。
- 默认缓存 10 秒。
- 空 endpoint 列表不缓存，避免服务刚注册或健康状态恢复后仍等待缓存过期。
- 查询失败时不吞异常，直接让调用失败暴露。
- 暂不做 stale cache fallback，避免隐藏 Consul 或服务状态问题。

后续如果真实运行需要更高可用性，再评估：

- Consul 不可用时短时间使用最后一次成功结果。
- 结合 health check watch 主动刷新。
- 配合 Static provider 做 fallback。

### 与 Static 的关系

`ServiceDiscovery.Static` 和 `ServiceDiscovery.Consul` 是并列 provider。

推荐：

- 服务间 HTTP 默认使用 Consul。
- 本地开发、单机联调和部署环境保持同一套 Consul 发现语义。
- Static provider 只作为特殊场景或测试场景的轻量 provider 保留。

第一版不做“Static fallback to Consul”或“Consul fallback to Static”的组合 provider。需要 fallback 时，应单独设计组合 resolver。

### 服务注册

服务自注册使用 Consul Agent HTTP API：

```text
PUT /v1/agent/service/register
PUT /v1/agent/service/deregister/{serviceId}
```

注册 payload 包含：

- `ID`
- `Name`
- `Address`
- `Port`
- `Meta.scheme`
- `Check.HTTP`
- `Check.Interval`
- `Check.DeregisterCriticalServiceAfter`

约定：

- `Check.HTTP` 由 `Scheme`、`Address`、`Port` 和 `HealthCheckPath` 组成。
- 默认健康检查路径使用 `/ready`，而不是 `/health/live`。
- `ServiceName` 必须使用稳定小写短名。
- `ServiceId` 必须能唯一标识一个运行实例。
- Docker Compose 多实例部署应开启 `UseInstanceDefaults`，由容器 hostname 和容器 IP 推导实例 id 与地址。
- Consul ACL token 如果配置，会同时用于发现和注册请求。
- 注册失败应让应用启动失败，避免服务没有注册却继续运行。
- 注销在 HostedService 停止时执行。

当前不做：

- TTL check。
- sidecar/proxy registration。
- blocking query / watch。
- 自动推导公网地址和端口。当前实例默认值只面向内部网络服务发现，不用于公网入口。

### 服务命名

服务名使用小写短名：

```text
identity
notification
gateway
```

不建议在服务名里包含环境、端口或实例号。

环境差异应由 Consul datacenter、namespace、部署配置或注册信息表达，而不是服务名表达。

## 当前不做

- 不接 Nacos、etcd。
- 不直接接 Kubernetes API。
- 不做独立服务注册 agent。
- 不做复杂权重、灰度或区域路由。
- 不把服务发现和 Resilience 合并成一个 BuildingBlock。
