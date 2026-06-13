# 缓存约定

本文档记录当前后端缓存抽象的最小约定。

## 项目结构

当前缓存 BuildingBlock 包含：

```text
BuildingBlocks/Data/Caching/
  LucidMicro.BuildingBlocks.Caching.Abstractions/
  LucidMicro.BuildingBlocks.Caching.Redis/
```

当前只提供 Redis 实现，暂不接入任何业务服务。

## ICacheService

应用层如需使用缓存，应依赖 `LucidMicro.BuildingBlocks.Caching.Abstractions` 中的 `ICacheService`。

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
```

约定：

- `key` 必须由调用方保证非空且稳定。
- `GetAsync<T>()` 返回 `null` 表示缓存未命中。
- 第一版不区分“缓存命中但值为 null”和“缓存未命中”，实现层不应主动缓存 null 值。
- `ttl` 为 `null` 时不设置过期时间。

## Redis 实现

Redis 实现项目为 `LucidMicro.BuildingBlocks.Caching.Redis`。

配置节：

```json
{
  "Lucid": {
    "Caching": {
      "Redis": {
        "ConnectionString": "localhost:6379"
      }
    }
  }
}
```

注册入口：

```csharp
services.AddLucidRedisCaching(
    configuration.GetRequiredSection(LucidRedisCacheOptions.ConfigurationSectionName));
```

当前 Redis 实现：

- 使用 `StackExchange.Redis`。
- 注册 `IConnectionMultiplexer` 为 singleton。
- 注册 `ICacheService` 为 singleton。
- 使用 `System.Text.Json` 序列化缓存值。

## Redis 相关 BuildingBlocks

Redis 连接由 `LucidMicro.BuildingBlocks.Caching.Redis` 统一注册。其他 Redis 相关 BuildingBlock 应复用容器中的同一个 `IConnectionMultiplexer`，不要重复创建 Redis 连接。

推荐注册顺序：

```csharp
services.AddLucidRedisCaching(
    configuration.GetRequiredSection(LucidRedisCacheOptions.ConfigurationSectionName));

services.AddLucidRedisHealthCheck();

services.AddLucidRedisDistributedLock();
```

其中：

- `AddLucidRedisCaching(...)` 注册 `IConnectionMultiplexer` 和 `ICacheService`。
- `AddLucidRedisHealthCheck()` 注册 Redis ready check，依赖已有 `IConnectionMultiplexer`。
- `AddLucidRedisDistributedLock()` 注册 Redis 分布式锁，依赖已有 `IConnectionMultiplexer`。

业务服务按需注册后两项。如果服务只需要缓存能力，只注册 `AddLucidRedisCaching(...)` 即可。

## Redis Health Check

Redis 缓存实现和 Redis 健康检查保持分离：

- `LucidMicro.BuildingBlocks.Caching.Redis` 负责注册 Redis 连接和 `ICacheService`。
- `LucidMicro.BuildingBlocks.HealthChecks.Redis` 负责注册 Redis ready check。

业务服务如果只需要缓存能力，只注册 `AddLucidRedisCaching(...)` 即可。

业务服务如果需要在 `/ready` 中检查 Redis，则在已有 `IConnectionMultiplexer` 的基础上额外注册：

```csharp
services.AddLucidRedisHealthCheck();
```

Redis health check 默认名为 `redis`，并带有 `ready`、`cache`、`redis` tags。

## 当前边界

当前暂不支持：

- Memory 实现。
- key prefix / namespace 策略。
- 缓存 null 值。
- 批量读写。
- 分布式锁。
- 缓存穿透、击穿、雪崩治理策略。
