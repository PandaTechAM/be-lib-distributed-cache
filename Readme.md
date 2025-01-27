# Pandatech.DistributedCache

**Pandatech.DistributedCache** is a lightweight .NET library that leverages `StackExchange.Redis` for distributed
caching.
Built on top of `StackExchange.Redis.Extensions.AspNetCore` and `StackExchange.Redis.Extensions.MsgPack`, it offers a
straightforward solution for typed caching, distributed locking, rate limiting, and stampede protection—all through the
`Microsoft.Extensions.Caching.Abstractions` NuGet package's `HybridCache` abstract class.

> Note: As of January 29, 2025, `HybridCache` is still in preview, and Microsoft does not provide an official
> implementation. The only known library with a `HybridCache` implementation is `FusionCache`, which uses a two-level (
> L1 +
> L2) caching model. While that approach can yield high performance, it also adds complexity—particularly in distributed
> environments. Many scenarios do not require such complexity, which is why Pandatech.DistributedCache avoids
> maintaining
> an L1 cache. Consequently, certain `HybridCacheEntryFlags` (e.g., disabling local cache writes) are effectively
> ignored in
> this library. You may set them, but they have no effect here.

Overall, `Pandatech.DistributedCache` weighs in at fewer than 500 lines of code, making it easy to understand, extend,
and
maintain.

## Features

- **Typed Cache Service:**
  Offers strongly typed caching using MessagePack serialization under the hood.
- **Distributed Locking:**
  Provides safe concurrency control with Redis-based locks.
- **Distributed Rate Limiting:**
  Allows you to apply business logic–driven rate limits on operations (e.g., sending SMS or email).
- **Stampede Protection:**
  Prevents a cache stampede by synchronizing concurrent `GetOrCreateAsync` calls on the same key.
- **HybridCache Integration:**
  Implements the preview `HybridCache` abstraction from Microsoft, ensuring a future-friendly approach if you choose to
  migrate to another HybridCache-based library in the future.
- **HybridCache Extension:**
  Provides extra methods for `HybridCache` to simplify common cache operations.
- **Health Check Integration:**
  Automatically registers a Redis health check (using `AspNetCore.HealthChecks.Redis`) for seamless readiness and
  liveness checks.

## Installation

Install the package from NuGet:

```bash
dotnet add package Pandatech.DistributedCache
```

## Usage

### 1. Configuration

In your `Program.cs`, configure the distributed cache:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddDistributedCache(options =>
{
    options.RedisConnectionString = "your_redis_connection_string"; //No default value and required
    options.ChannelPrefix = "your_channel_prefix"; //Default is null
    options.ConnectRetry = 15; //Default is 10
    options.ConnectTimeout = TimeSpan.FromSeconds(10); //Default is 10 seconds
    options.SyncTimeout = TimeSpan.FromSeconds(5); //Default is 5 seconds
    options.DistributedLockDuration = TimeSpan.FromSeconds(30); //Default is 8 seconds
    options.DefaultExpiration = TimeSpan.FromMinutes(5); //Default is 15 minutes
});

var app = builder.Build();
```

When `AddDistributedCache` is called:

- A Redis connection is established with the specified parameters.
- A Redis health check is automatically registered with a 3-second timeout, ensuring that your application can properly
  monitor Redis availability.

### 2. Cached Entity Preparation

Create a model class to store in the cache. Decorate it with `[MessagePackObject]` so it can be serialized and
deserialized with MessagePack:

```csharp
[MessagePackObject]
public class TestCacheEntity : ICacheEntity
{
    [Key(0)] public string Name { get; set; } = "Bob";
    [Key(1)] public int Age { get; set; } = 15;
    [Key(2)] public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

### 3. Injecting HybridCache

Use dependency injection to retrieve an instance of `HybridCache` and perform cache operations:

```csharp
public class CacheTestsService(HybridCache hybridCache)
{
   public async Task GetFromCache(CancellationToken token = default)
   {
      var call1 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5),
         },
         ["test"],
         token);

     

      var call2 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5),
         },
         ["test"],
         token);

      var call3 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test2",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5),
         },
         ["vazgen"],
         token);

    

      var call4 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test3",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5),
         },
         ["test", "vazgen"],
         token);
      
   }
   
   public async Task TestExistence(CancellationToken token = default)
   {
      var call1Check = await hybridCache.ExistsAsync<TestCacheEntity>("test", token);
      Console.WriteLine($"Call1: {call1Check}");
      var call2Check = await hybridCache.ExistsAsync<TestCacheEntity>("test", token);
      Console.WriteLine($"Call2: {call2Check}");
      var call3Check = await hybridCache.ExistsAsync<TestCacheEntity>("test2", token);
      Console.WriteLine($"Call3: {call3Check}");
      var call4Check = await hybridCache.ExistsAsync<TestCacheEntity>("test3", token);
      Console.WriteLine($"Call4: {call4Check}");
   }

   public async Task DeleteCache(CancellationToken token = default)
   {
      await hybridCache.RemoveByTagAsync("test", token);
   }

   public async Task<TestCacheEntity> GetFromPostgres(CancellationToken token)
   {
      Console.WriteLine("Hey, I'm Fetching from postgres");
      await Task.Delay(500, token);
      return new TestCacheEntity();
   }
}
```

### 4. Rate Limiting

Pandatech.DistributedCache also supports rate limiting via `IRateLimitService`.

**Example Rate Limit Configuration**
Use an enum to track different action types, and create a shared configuration:

```csharp
public enum ActionType //your business logic actions
{
    SmsForTfa = 1,
    EmailForTfa = 2
}

public static class RateLimitingConfigurations //your shared rate limiting configuration
{
    public static RateLimitConfiguration GetSmsConfig()
    {
        return new RateLimitConfiguration
        {
            ActionType = (int)ActionType.SmsForTfa,
            MaxAttempts = 2,
            TimeToLive = TimeSpan.FromSeconds(10)
        };
    }
}
```

**Applying Rate Limiting**

```csharp
using DistributedCache.Dtos;
using DistributedCache.Services.Interfaces;

public class SendSmsService(IRateLimitService rateLimitService)
{
    public async Task<RateLimitState> SendSms(CancellationToken cancellationToken = default)
    {
        var phoneNumber = "1234567890";
        var rateLimitConfiguration = RateLimitingConfigurations.GetSmsConfig().SetIdentifiers(phoneNumber);

        return await rateLimitService.RateLimitAsync(rateLimitConfiguration, cancellationToken);
    }
}
```

### 5. Distributed Locking

Distributed locks can be used for concurrency control. The core interface:

```csharp
public interface IDistributedLockService
{
   Task<bool> AcquireLockAsync(string resourceKey, string lockToken);
   Task<bool> HasLockAsync(string resourceKey);
   Task WaitUntilLockIsReleasedAsync(string resourceKey, CancellationToken cancellationToken);
   Task ReleaseLockAsync(string resourceKey, string lockToken);
}
```

In practice, you inject `IDistributedLockService` into your service or use the default RedisLockService, acquire a lock
for a specific resource, and release it once your operation completes. This ensures that only one caller can modify a
given resource at a time.

### 6. Health Check Integration

`Pandatech.DistributedCache` automatically adds a Redis health check to your application through the
`AddDistributedCache`
method. By default, the check uses a 3-second timeout to validate Redis connectivity. This helps with
container-orchestrated environments (Kubernetes, Docker, etc.) to ensure your service is only considered healthy when
Redis is accessible.

### 7. HybridCache Extensions

Pandatech.DistributedCache provides a few helpful extension methods for the `HybridCache` abstraction. These extensions
are also compatible with other `HybridCache` implementations you might use in the future, allowing for a smoother
migration path if you switch providers.

1. `GetOrDefaultAsync<TValue>` Retrieves an item from the cache using the specified key. If the item is not found, it
   returns the provided default
   value instead of creating a real cache entry:
   ```csharp
    var cachedValue = await hybridCache.GetOrDefaultAsync("someKey", defaultValue, cancellationToken);
    ```
   This is especially useful when you simply want a fallback value without messing up with factory methods.
2. `TryGetAsync<TValue>`
   Attempts to retrieve an item from the cache. If it exists, returns (true, value), otherwise (false, default).
   ```csharp
    var (exists, value) = await hybridCache.TryGetAsync<YourModel>("someKey", cancellationToken);
    if (exists)
    {
    // use the 'value'
    }
    else
    {
    // handle the 'not found' case
    }
    ```
   This method is a cleaner alternative to a typical “check then get” pattern, eliminating extra round-trips to Redis.
3. `ExistsAsync<TValue>`
   Quickly checks if an item exists in the cache without returning its value:
   ```csharp
    var recordExists = await hybridCache.ExistsAsync<YourModel>("someKey", cancellationToken);
    if (recordExists)
    {
    // Key is present in cache
    }
    else
    {
    // Key does not exist
    }
    ```
   Useful in scenarios where you only need to confirm the existence of the key rather than retrieve and deserialize its
   data.

## Enforced MessagePack Serialization

We enforce MessagePack serialization to maximize performance and simplicity:

1. **Speed:** MessagePack can be several times faster than JSON and faster than Protobuf in many scenarios.
2. **Compactness:** MessagePack typically produces smaller payloads than JSON, improving network transfer performance
   and
   memory usage.
3. **Tooling Support:** Many Redis management tools (e.g., Another Redis Desktop Manager) can display MessagePack data
   as JSON-like view for easy debugging.
4. **Consistency:** A single, enforced serialization format avoids complexity and ensures consistent behavior across
   your
   application.

**Benchmark Snapshot:**
-------------------------

| Format      | Serialization Speed   | Deserialization Speed | Serialized Size |
|-------------|-----------------------|-----------------------|-----------------|
| MessagePack | 4x faster than JSON   | 3x faster than JSON   | ~50% of JSON    |
| Protobuf    | 1.5x faster than JSON | 1.2x faster than JSON | ~70% of JSON    |
| JSON        | Baseline              | Baseline              | Baseline        |

Because of these advantages, `Pandatech.DistributedCache` does not provide alternative serialization options;
MessagePack
covers the core needs effectively.

## License

Pandatech.DistributedCache is licensed under the MIT License.

----------------
`Pandatech.DistributedCache` aims to simplify distributed caching for most .NET applications without incurring the
overhead of a multi-level caching system. If your application requirements evolve, the standardized HybridCache
abstraction ensures you can switch to other providers (such as `FusionCache` or Microsoft's future implementation)
without extensive refactoring. Enjoy fasterlookups, simpler code, and safer concurrency management—all in under 500
lines of code.