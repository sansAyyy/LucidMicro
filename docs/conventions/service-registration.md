# 服务启动注册约定

本文档定义各业务服务在 `Api`、`Application`、`Infrastructure` 三层中的注册边界。

目标是让 `Program.cs` 保持简洁，让具体基础设施实现集中在服务自己的 Infrastructure 层组合。

## 基本规则

服务启动入口应优先保持以下形态：

```csharp
builder.Services.AddXxxApplication();
builder.Services.AddXxxInfrastructure(builder.Configuration);
```

`Program.cs` 可以注册 ASP.NET Core 宿主级能力，例如：

- controllers
- problem details
- exception handler
- OpenAPI
- CORS
- health checks endpoint
- authentication / authorization middleware
- logging middleware

`Program.cs` 不应直接组合业务用例所需的具体基础设施实现。

## Application

Application 层负责注册应用服务、验证器和应用层用例。

Application 可以依赖：

- Domain
- BuildingBlock `.Abstractions`
- BuildingBlock `.Core`
- Contracts

Application 不应依赖具体实现项目，例如：

- `Caching.Redis`
- `Persistence.EFCore`
- `EventBus.RabbitMQ`
- `Auth.AspNetCore`
- `ServiceDiscovery.Consul`

如果应用服务需要能力，应依赖端口接口，例如 `IPasswordHashingService`、`ICacheService`、`IRepository<T>`、`INotificationClient`。

## Infrastructure

Infrastructure 层负责组合 Application 端口的具体实现。

常见内容：

- EF Core persistence
- Redis cache / store
- Outbox / Inbox EF Core
- service-to-service HTTP client
- service discovery provider
- service registration provider
- resilience policy
- auth provider implementation
- external provider adapter
- infrastructure-level health check

例如 Identity 的密码哈希、当前用户、JWT token service 都是 Application 依赖的 Auth 端口实现，因此由 `Identity.Infrastructure` 注册，而不是由 `Identity.Api` 直接注册。

推荐把 `AddXxxInfrastructure(...)` 内部按职责拆成私有方法：

```csharp
public static IServiceCollection AddXxxInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    AddPersistence(services, configuration);
    AddAuth(services, configuration);
    AddMessaging(services, configuration);
    AddExternalClients(services, configuration);

    return services;
}
```

私有方法只用于整理职责，不作为额外对外注册入口。服务外部仍只调用 `AddXxxInfrastructure(...)`。

## Api

Api 层负责 ASP.NET Core 宿主、HTTP 管道和交付层配置。

Api 可以注册：

- `AddControllers()`
- `AddProblemDetails()`
- `AddExceptionHandler<T>()`
- `AddLucidOpenApi(...)`
- `AddLucidCors(...)`
- `AddLucidHealthChecks()`
- `AddAuthorization()`
- `UseAuthentication()`
- `UseAuthorization()`
- `MapControllers()`
- `MapLucidHealthChecks()`

Api 不应直接注册 Application 端口的实现。

例如：

- `AddLucidJwtAuthentication(...)` 属于 Auth 实现注册，应放在服务 Infrastructure。
- `UseAuthentication()` 属于 HTTP 管道，应放在 Api。
- `AddLucidRedisCaching(...)` 如果是为了实现某个应用端口，应放在服务 Infrastructure。
- `AddLucidCors(...)` 是宿主 HTTP 策略，应放在 Api。

## BuildingBlock 注册位置

不是所有 BuildingBlock 都固定注册在 Api 或 Infrastructure，取决于它承担的职责。

放在 Api 的典型能力：

- ASP.NET Core exception handling
- OpenAPI / Scalar
- CORS
- HTTP middleware
- endpoint mapping

放在 Infrastructure 的典型能力：

- persistence provider
- cache provider
- distributed lock provider
- auth implementation provider
- service discovery provider
- resilience for typed external clients
- Inbox / Outbox storage
- external system adapter

如果某个 BuildingBlock 同时包含服务注册和 middleware，通常拆开处理：

```text
AddXxx(...)  选择实现和注册服务，通常在 Infrastructure
UseXxx(...)  HTTP 管道动作，通常在 Api
```

## 测试约定

服务注册边界调整时，应至少覆盖：

- `AddXxxInfrastructure(...)` 能注册 Application 依赖的关键端口实现。
- `Program.cs` 不直接引用不需要的具体实现项目。
- API 合约测试仍能通过。
- 对应服务的 Infrastructure 测试仍能通过。

当某个实现从 Api 移到 Infrastructure 时，Infrastructure 测试配置也应补齐该实现所需的配置节。

## 当前服务形态

Identity：

- `Identity.Application` 注册管理员、认证、短信登录等应用服务。
- `Identity.Infrastructure` 注册 EF Core、Outbox、Auth.AspNetCore、Redis、Resilience、Consul ServiceDiscovery、Consul service registration、Consul/Npgsql/RabbitMQ health check、Notification HTTP client、RabbitMQ EventBus 和 Outbox publisher hosted service。
- `Identity.Api` 注册 ASP.NET Core 宿主能力和 HTTP 管道。

Notification：

- `Notification.Application` 注册通知应用服务和 consumer handler。
- `Notification.Infrastructure` 注册 EF Core、Inbox、Consul ServiceDiscovery、Consul service registration、RabbitMQ EventBus、RabbitMQ consumer、Consul/Npgsql/RabbitMQ health check、发送渠道实现。
- `Notification.Api` 注册 OpenAPI、CORS、Health Checks endpoint 和 HTTP endpoint。
