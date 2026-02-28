# Pandatech.DistributedCache

A focused .NET library that implements Microsoft's `HybridCache` abstraction on top of Redis. Provides strongly typed
caching with MessagePack serialization, distributed locking, stampede protection, tag-based invalidation, and rate
limiting — in under 500 lines of code.

Targets **`net9.0`** and **`net10.0`** only. `HybridCache` graduated from preview in .NET 9; net8 is not supported.

---

## Table of Contents

1. [Features](#features)
2. [Installation](#installation)
3. [Getting Started](#getting-started)
4. [Caching](#caching)
5. [Tag-Based Invalidation](#tag-based-invalidation)
6. [HybridCache Extensions](#hybridcache-extensions)
7. [Rate Limiting](#rate-limiting)
8. [Distributed Locking](#distributed-locking)
9. [String Extensions](#string-extensions)
10. [Configuration Reference](#configuration-reference)
11. [MessagePack Serialization](#messagepack-serialization)
12. [Health Check](#health-check)

---

## Features

- **HybridCache implementation** — backs Microsoft's `HybridCache` abstraction with Redis, no local L1 layer
- **Stampede protection** — concurrent `GetOrCreateAsync` calls on the same key are serialized; only one caller hits
  the factory
- **Tag-based invalidation** — group cache entries under one or more tags and invalidate them all in one call
- **Distributed locking** — Redis-backed `IDistributedLockService` with atomic acquire/release via Lua
- **Rate limiting** — business-logic-level rate limiting with per-action, per-identity counters
- **MessagePack serialization** — binary, compact, fast; enforced uniformly across all cache entries
- **`HybridCache` extension methods** — `GetOrDefaultAsync`, `TryGetAsync`, `ExistsAsync`
- **String key helpers** — `PrefixWithAssemblyName` and `PrefixWith` for structured, collision-safe key naming
- **Redis health check** — auto-registered with a 3-second timeout on `AddDistributedCache`

---

## Installation

```bash
dotnet add package Pandatech.DistributedCache
```

---

## Getting Started

One call in `Program.cs` wires everything up:

```csharp
builder.AddDistributedCache(options =>
{
    options.RedisConnectionString = "localhost:6379";   // required
    options.ChannelPrefix         = "myapp";            // optional, default: null
});
```

`AddDistributedCache` registers:

- `IConnectionMultiplexer` (singleton, with exponential reconnect)
- `HybridCache` → `RedisDistributedCache` (singleton)
- `IRateLimitService` → `RedisRateLimitService` (singleton)
- `IDistributedLockService` → `RedisLockService` (singleton)
- Redis health check with a 3-second timeout

---

## Caching

### Preparing a cached model

Decorate your model with `[MessagePackObject]` and implement `ICacheEntity`:

```csharp
[MessagePackObject]
public class UserSessionCache : ICacheEntity
{
    [Key(0)] public Guid UserId { get; set; }
    [Key(1)] public string Role { get; set; } = string.Empty;
    [Key(2)] public DateTime ExpiresAt { get; set; }
}
```

`ICacheEntity` is a marker interface with no members. It exists to make the intent explicit at the type level.

### GetOrCreateAsync

Inject `HybridCache` directly. If the key is absent, the factory runs once — concurrent callers block until the first
writer is done (stampede protection):

```csharp
public class SessionService(HybridCache cache)
{
    public async Task<UserSessionCache> GetSessionAsync(Guid userId, CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(
            $"session:{userId}",
            async _ => await LoadFromDbAsync(userId, ct),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(30) },
            tags: [$"user:{userId}"],
            cancellationToken: ct);
    }
}
```

### SetAsync

```csharp
await cache.SetAsync(
    $"session:{userId}",
    session,
    new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(30) },
    tags: [$"user:{userId}"],
    cancellationToken: ct);
```

If `Expiration` is omitted, `DefaultExpiration` from configuration is used (default: 15 minutes). Pass
`TimeSpan.MaxValue` to store without an expiry.

### RemoveAsync

```csharp
await cache.RemoveAsync($"session:{userId}", ct);
```

---

## Tag-Based Invalidation

Tags let you invalidate a group of related entries without knowing their individual keys. Calling `RemoveByTagAsync`
writes a tombstone timestamp for that tag. The next read of any entry carrying that tag checks the tombstone — if the
tag was updated after the entry was written, the entry is evicted and re-fetched.

```csharp
// Invalidate all cache entries tagged with "user:{userId}"
await cache.RemoveByTagAsync($"user:{userId}", ct);
```

An entry can carry multiple tags:

```csharp
tags: ["user:42", "tenant:7"]
```

Invalidating either tag is enough to evict the entry on next read.

---

## HybridCache Extensions

Three extension methods on `HybridCache` cover the most common patterns that the base API handles awkwardly.

### GetOrDefaultAsync

Returns a cached value or a caller-supplied default without writing anything to Redis:

```csharp
var value = await cache.GetOrDefaultAsync("feature-flag:dark-mode", defaultValue: false, ct);
```

### TryGetAsync

Returns whether the key exists alongside its value in one round-trip:

```csharp
var (exists, session) = await cache.TryGetAsync<UserSessionCache>($"session:{userId}", ct);
if (!exists)
{
    // key is not in cache
}
```

### ExistsAsync

Checks presence without deserializing the value:

```csharp
var isActive = await cache.ExistsAsync<UserSessionCache>($"session:{userId}", ct);
```

All three extensions are implemented against the `HybridCache` abstraction, so they work with any compatible
implementation — not just this one.

---

## Rate Limiting

`IRateLimitService` applies business-logic rate limits per action type and identity. State is stored in Redis and is
consistent across all instances of your service.

### Define action types and configurations

```csharp
public enum ActionType
{
    SmsOtp   = 1,
    EmailOtp = 2,
    Login    = 3
}

public static class RateLimits
{
    public static RateLimitConfiguration SmsOtp() => new()
    {
        ActionType  = (int)ActionType.SmsOtp,
        MaxAttempts = 3,
        TimeToLive  = TimeSpan.FromMinutes(10)
    };

    public static RateLimitConfiguration Login() => new()
    {
        ActionType  = (int)ActionType.Login,
        MaxAttempts = 10,
        TimeToLive  = TimeSpan.FromMinutes(15)
    };
}
```

### Apply the limit

```csharp
public class AuthService(IRateLimitService rateLimitService)
{
    public async Task<RateLimitState> RequestOtpAsync(string phoneNumber, CancellationToken ct = default)
    {
        var config = RateLimits.SmsOtp().SetIdentifiers(phoneNumber);
        var state  = await rateLimitService.RateLimitAsync(config, ct);

        if (state.Status == RateLimitStatus.Exceeded)
        {
            // state.TimeToReset  — how long until the window resets
            // state.RemainingAttempts — always 0 here
            throw new TooManyRequestsException($"Try again in {state.TimeToReset.TotalSeconds:0}s.");
        }

        // state.RemainingAttempts — how many calls are left in the window
        await SendSmsAsync(phoneNumber, ct);
        return state;
    }
}
```

`SetIdentifiers` takes a primary identifier (e.g. phone number) and an optional secondary identifier (e.g. tenant ID).
The two together form a unique rate-limit key for that action type.

`RateLimitState` always contains:

| Property            | Meaning                                                |
|---------------------|--------------------------------------------------------|
| `Status`            | `NotExceeded` or `Exceeded`                            |
| `TimeToReset`       | Remaining TTL of the current window                    |
| `RemainingAttempts` | Calls left before `Exceeded` (0 when already exceeded) |

---

## Distributed Locking

`IDistributedLockService` is available for cases where you need explicit locking outside of the cache layer. The
implementation uses `SET NX` for acquire and a Lua script for atomic release — the standard Redis lock pattern.

```csharp
public class InventoryService(IDistributedLockService locks)
{
    public async Task DeductStockAsync(int productId, int quantity, CancellationToken ct = default)
    {
        var key   = $"product:{productId}";
        var token = Guid.NewGuid().ToString();

        if (!await locks.AcquireLockAsync(key, token))
        {
            await locks.WaitUntilLockIsReleasedAsync(key, ct);
            // re-read state and decide what to do
            return;
        }

        try
        {
            // exclusive access to this product's stock
        }
        finally
        {
            await locks.ReleaseLockAsync(key, token);
        }
    }
}
```

| Method                         | Behaviour                                                                                                       |
|--------------------------------|-----------------------------------------------------------------------------------------------------------------|
| `AcquireLockAsync(key, token)` | Returns `true` if the lock was taken; `false` if already held by another caller                                 |
| `HasLockAsync(key)`            | Returns `true` if any lock currently exists on this key                                                         |
| `WaitUntilLockIsReleasedAsync` | Polls every 10 ms; throws `TimeoutException` if the lock isn't released within `2 × DistributedLockMaxDuration` |
| `ReleaseLockAsync(key, token)` | Releases the lock only if the stored token matches; safe against accidental cross-caller release                |

---

## String Extensions

Utilities for building structured, collision-safe Redis key names.

```csharp
// Prefix with a literal string
"user:42".PrefixWith("myapp");                    // → "myapp:user:42"

// Prefix with the calling assembly's name (resolved at call site)
"user:42".PrefixWithAssemblyName();               // → "MyService.Api:user:42"

// Batch prefix
new[] { "user:1", "user:2" }.PrefixWith("myapp"); // → ["myapp:user:1", "myapp:user:2"]
new[] { "user:1", "user:2" }.PrefixWithAssemblyName();
```

`PrefixWithAssemblyName` calls `Assembly.GetCallingAssembly()`, so it captures the assembly that actually calls the
method — useful for shared utilities that should tag keys with the service that owns them.

---

## Configuration Reference

All options except `RedisConnectionString` have sensible defaults and are optional.

| Option                       | Type       | Default | Description                                                                     |
|------------------------------|------------|---------|---------------------------------------------------------------------------------|
| `RedisConnectionString`      | `string`   | —       | **Required.** Standard StackExchange.Redis connection string.                   |
| `ChannelPrefix`              | `string?`  | `null`  | Optional namespace prefix inserted between `DistributedCache` and your key.     |
| `ConnectRetry`               | `int`      | `10`    | Number of connection retries on startup.                                        |
| `ConnectTimeout`             | `TimeSpan` | `10s`   | Timeout for establishing a connection.                                          |
| `SyncTimeout`                | `TimeSpan` | `5s`    | Timeout for synchronous Redis commands.                                         |
| `DistributedLockMaxDuration` | `TimeSpan` | `8s`    | TTL applied to each lock key. Also governs the wait timeout (`2 ×` this value). |
| `DefaultExpiration`          | `TimeSpan` | `15min` | Fallback TTL when no `Expiration` is supplied in `HybridCacheEntryOptions`.     |

### Key naming

All cache keys are stored in Redis under the pattern:

```
DistributedCache[:{ChannelPrefix}]:{yourKey}
```

Tag tombstone keys follow:

```
DistributedCache[:{ChannelPrefix}]:tag:{tagName}
```

Lock keys append `:lock` to the prefixed cache key.

---

## MessagePack Serialization

All cache values are serialized with MessagePack. This is not configurable — by design.

MessagePack is binary, compact (~50% of equivalent JSON), and significantly faster to serialize and deserialize than
JSON or Protobuf in most .NET benchmarks. It also renders as a JSON-like view in most Redis desktop clients (e.g.
Another Redis Desktop Manager), so debugging is not meaningfully harder than with JSON.

Enforcing a single serializer removes an entire class of subtle bugs (mismatched serializers between writers and
readers, type name handling differences, DateTime encoding differences) and keeps the library surface small.

The trade-off: your cached models must carry `[MessagePackObject]` and `[Key(n)]` attributes. This is a one-time,
mechanical annotation and does not affect your domain logic.

---

## Health Check

`AddDistributedCache` automatically registers a Redis health check via `AspNetCore.HealthChecks.Redis` with a 3-second
timeout. No additional configuration is needed.

If you expose a health endpoint:

```csharp
app.MapHealthChecks("/health");
```

Redis connectivity is included in the response automatically. This integrates with Kubernetes liveness/readiness probes,
load-balancer health checks, and any monitoring stack that speaks the ASP.NET Core health check protocol.

---

## License

MIT