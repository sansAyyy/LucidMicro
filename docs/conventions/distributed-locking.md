# 分布式锁约定

本文档记录当前后端分布式锁抽象的最小约定。

## 项目结构

当前分布式锁 BuildingBlock 包含：

```text
BuildingBlocks/Data/DistributedLock/
  LucidMicro.BuildingBlocks.DistributedLock.Abstractions/
  LucidMicro.BuildingBlocks.DistributedLock.Redis/
```

暂不接入任何业务服务。

## IDistributedLockService

应用层如需使用分布式锁，应依赖 `LucidMicro.BuildingBlocks.DistributedLock.Abstractions` 中的 `IDistributedLockService`。

```csharp
public interface IDistributedLockService
{
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<IDistributedLockHandle?> WaitAcquireAsync(
        string key,
        TimeSpan expiry,
        TimeSpan waitTimeout,
        CancellationToken cancellationToken = default);
}
```

约定：

- `key` 必须由调用方保证非空且稳定。
- `expiry` 必填，具体实现应拒绝非正数过期时间，避免锁永久占用。
- `waitTimeout` 表示最多等待多久获取锁，具体实现应拒绝负数等待时间。
- `TryAcquireAsync()` 只尝试一次，返回 `null` 表示未获取到锁。
- `WaitAcquireAsync()` 会在 `waitTimeout` 内重复尝试，超时后返回 `null`。
- 返回 `IDistributedLockHandle` 表示已获取锁，调用方通过 `DisposeAsync()` 释放锁。
- 调用方应使用 `await using` 管理锁生命周期。

示例：

```csharp
await using var lockHandle = await distributedLock.TryAcquireAsync(
    "jobs:daily-report",
    TimeSpan.FromMinutes(1),
    cancellationToken);

if (lockHandle is null)
{
    return;
}

// do protected work
```

## Redis 实现

Redis 实现项目为 `LucidMicro.BuildingBlocks.DistributedLock.Redis`。

注册入口：

```csharp
services.AddLucidRedisDistributedLock();
```

Redis 分布式锁实现：

- 复用容器中已有的 `IConnectionMultiplexer`。
- 注册 `IDistributedLockService` 为 singleton。
- 获取锁使用 `StringSetAsync(key, token, expiry, When.NotExists)`。
- 释放锁使用 Lua 脚本先校验 token，再删除 key，避免误删其他持有者的锁。
- `WaitAcquireAsync()` 使用简单轮询，超时后返回 `null`。

Redis 实现不单独读取 Redis 配置。业务服务应先通过缓存 Redis BuildingBlock 或其他方式注册 `IConnectionMultiplexer`。

Redis 相关 BuildingBlocks 的推荐注册顺序见 `docs/conventions/caching.md`。

## 当前边界

当前暂不支持：

- 自动续期。
- 重入锁。
- 公平锁。
- 多资源锁。
