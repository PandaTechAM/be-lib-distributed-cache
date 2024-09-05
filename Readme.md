# Pandatech.DistributedCache

Pandatech.DistributedCache is a .NET library providing an efficient and performant abstraction layer over
`StackExchange.Redis`, specifically designed for .NET applications. This library builds on top of
`StackExchange.Redis.Extensions.AspNetCore` and `StackExchange.Redis.Extensions.MsgPack` to offer a robust, easy-to-use
caching solution with advanced features such as typed cache services, distributed locking, business logic rate limiting.

## Features

- **Typed Cache Service:** Supports strongly-typed caching with MessagePack serialization.
- **Distributed Locking:** Ensures data consistency with distributed locks.
- **Distributed Rate Limiting:** Prevents cache abuse with rate limiting based on business logic.
- **Key Isolation:** Modular monolith support by prefixing keys with assembly names.
- **Stampede Protection:** Protects against cache stampede in the `GetOrCreateAsync` method.
- **No Serializer Override:** Enforces MessagePack serialization for performance and readability.

## Installation

Add `Pandatech.DistributedCache` to your project using NuGet:

```bash
dotnet add package Pandatech.DistributedCache
```

## Usage

### 1. Configuration

In your `Program.cs`, configure the cache service:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddDistributedCache(options =>
{
    options.RedisConnectionString = "your_redis_connection_string"; //No default value and required
    options.ConnectRetry = 15; //Default is 10
    options.ConnectTimeout = TimeSpan.FromSeconds(10); //Default is 10 seconds
    options.SyncTimeout = TimeSpan.FromSeconds(5); //Default is 5 seconds
    options.DistributedLockDuration = TimeSpan.FromSeconds(5); //Default is 5 seconds
    options.DefaultExpiration = TimeSpan.FromMinutes(5); //Default is 15 minutes
});

var app = builder.Build();
```

#### Advanced Configuration

**Key Prefix for Isolation**
To ensure module-level isolation in modular monoliths, use the `KeyPrefixForIsolation` setting. This will not allow
cross ClassLibrary cache access.

```csharp
options.KeyPrefixForIsolation = KeyPrefix.AssemblyNamePrefix;
```

**Note:** Even if you don't use key prefixing, you still need to provide the class as a generic type (`T`) when using
`IRateLimitService<T>`. The generic type `T` is used to retrieve the assembly name, which is important for key isolation. If
you choose not to prefix keys by assembly name, this type is still required but will be ignored in the actual
implementation.

### 2. Cached Entity Preparation

Create your cache entity/model in order to inject it in the actual service:

```csharp
[MessagePackObject]
public class TestCacheEntity : ICacheEntity
{
    [Key(0)] public string Name { get; set; } = "Bob";
    [Key(1)] public int Age { get; set; } = 15;
    [Key(2)] public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

### 3. Injecting ICacheService

Use `ICacheService<PreparedCacheEntity>` in your services to interact with the cache:

```csharp
public class CacheTestsService(ICacheService<TestCacheEntity> cacheService)
{
    public async Task GetFromCache(CancellationToken token = default)
    {
        await cacheService.GetOrCreateAsync("test",
            async _ => await GetFromPostgres(token),
            TimeSpan.FromMinutes(1),
            ["test"],
            token);

        await cacheService.GetOrCreateAsync("test2",
           async _ => await GetFromPostgres(token),
           TimeSpan.FromMinutes(1),
           ["vazgen"],
           token);
        
        await cacheService.GetOrCreateAsync("test3",
           async _ => await GetFromPostgres(token),
           TimeSpan.FromMinutes(1),
           ["test", "vazgen"],
           token);
    }

    public async Task DeleteCache(CancellationToken token = default)
    {
        await cacheService.RemoveByTagAsync("test", token);
    }

    public async Task<TestCacheEntity> GetFromPostgres(CancellationToken token)
    {
        Console.WriteLine("Fetching from PostgreSQL");
        await Task.Delay(500, token);
        return new TestCacheEntity();
    }
}
```

### 4. Interface Methods

```csharp
namespace DistributedCache.Services.Interfaces;

/// <summary>
/// Interface for cache service operations.
/// </summary>
/// <typeparam name="T">The type of the cache entity.</typeparam>
public interface ICacheService<T> where T : class
{
   /// <summary>
   /// Gets or creates a cache entry asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry.</param>
   /// <param name="factory">A factory function to create the cache entry if it does not exist.</param>
   /// <param name="expiration">Optional expiration time for the cache entry.</param>
   /// <param name="tags">Optional tags associated with the cache entry.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation, with the cache entry as the result.</returns>
   ValueTask<T> GetOrCreateAsync(string key, Func<CancellationToken, ValueTask<T>> factory,
      TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default);

   /// <summary>
   /// Gets a cache entry asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation, with the cache entry as the result if found; otherwise, null.</returns>
   ValueTask<T?> GetAsync(string key, CancellationToken token = default);

   /// <summary>
   /// Sets a cache entry asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry.</param>
   /// <param name="value">The value of the cache entry.</param>
   /// <param name="expiration">Optional expiration time for the cache entry.</param>
   /// <param name="tags">Optional tags associated with the cache entry.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   ValueTask SetAsync(string key, T value, TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null,
      CancellationToken token = default);

   /// <summary>
   /// Removes a cache entry by key asynchronously.
   /// </summary>
   /// <param name="key">The key of the cache entry to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   ValueTask RemoveByKeyAsync(string key, CancellationToken token = default);

   /// <summary>
   /// Removes multiple cache entries by their keys asynchronously.
   /// </summary>
   /// <param name="keys">The keys of the cache entries to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   ValueTask RemoveByKeysAsync(IEnumerable<string> keys, CancellationToken token = default);

   /// <summary>
   /// Removes cache entries associated with a tag asynchronously.
   /// </summary>
   /// <param name="tag">The tag associated with the cache entries to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   /// <remarks>
   /// If multiple tags are specified, any entry matching any one of the tags will be removed. This means tags are treated as an "OR" condition.
   /// </remarks>
   ValueTask RemoveByTagAsync(string tag, CancellationToken token = default);

   /// <summary>
   /// Removes cache entries associated with multiple tags asynchronously.
   /// </summary>
   /// <param name="tags">The tags associated with the cache entries to remove.</param>
   /// <param name="token">Cancellation token.</param>
   /// <returns>A task representing the asynchronous operation.</returns>
   /// <remarks>
   /// If multiple tags are specified, any entry matching any one of the tags will be removed. This means tags are treated as an "OR" condition.
   /// </remarks>
   ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken token = default);
}
```

### 5. Rate Limiting

Implement rate limiting using `IRateLimitService` and `RateLimitConfiguration`.

**Define Rate Limiting Configuration**

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

**Implement Rate Limiting in the service**

```csharp
using DistributedCache.Dtos;
using DistributedCache.Services.Interfaces;

public class SendSmsService(IRateLimitService<SendSmsService> rateLimitService)
{
    public async Task<RateLimitState> SendSms(CancellationToken cancellationToken = default)
    {
        var phoneNumber = "1234567890";
        var rateLimitConfiguration = RateLimitingConfigurations.GetSmsConfig().SetIdentifiers(phoneNumber);

        return await rateLimitService.RateLimitAsync(rateLimitConfiguration, cancellationToken);
    }
}
```

Based on rate limit state you can throw exception/return 427 or proceed with the business logic.

## Enforced MessagePack Serialization

`Pandatech.DistributedCache` enforces the use of MessagePack serialization for several compelling reasons:

1. **Performance:** MessagePack is significantly faster compared to other serialization formats. For example, benchmarks
   show that MessagePack can be up to 4 times faster than JSON and 1.5 times faster than Protobuf in terms of
   serialization and deserialization speed.
2. **Compact Size:** MessagePack produces smaller payloads, which results in lower memory usage and faster data transfer
   over the network. On average, MessagePack serialized data is about 50% smaller than JSON and 20-30% smaller than
   Protobuf.
3. **Human Readability in Tools:** Many Redis clients, such as Another Redis Desktop Manager, can display MessagePack
   serialized data as JSON, making it easier for developers to inspect and debug the cache content.
4. **Simplicity:** By enforcing a single serialization format, we avoid the complexity and potential issues that can
   arise from supporting multiple serializers. This decision simplifies the implementation and ensures consistent
   behavior across different parts of the application.

Given these benefits, overriding the serializer is not provided as MessagePack meets the performance and usability needs
effectively.

**Benchmark Comparison**
-------------------------

| Format      | Serialization Speed   | Deserialization Speed | Serialized Size |
|-------------|-----------------------|-----------------------|-----------------|
| MessagePack | 4x faster than JSON   | 3x faster than JSON   | ~50% of JSON    |
| Protobuf    | 1.5x faster than JSON | 1.2x faster than JSON | ~70% of JSON    |
| JSON        | Baseline              | Baseline              | Baseline        |

## Acknowledgements

Inspired by Microsoft's .NET 9 `HybridCache` and leveraging the power of `StackExchange.Redis`. `HybridCache` is in a
preview state and is not recommended for production use. The main difference is that `HybridCache` is too general and
also
uses L1 + L2 caching instead of only L2 caching.

When the time comes and `HybridCache` will become stable, mature and feature rich, we will consider migrating to it with
backward compatability.

## License

Pandatech.DistributedCache is licensed under the MIT License.