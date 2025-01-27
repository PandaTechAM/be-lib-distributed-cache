using DistributedCache.Helpers;
using DistributedCache.Models;
using DistributedCache.Options;
using DistributedCache.Services.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace DistributedCache.Services.Implementations;

internal sealed class RedisDistributedCache(
   IRedisClient redisClient,
   IOptions<CacheConfigurationOptions> options,
   IDistributedLockService distributedLockService,
   ILogger<RedisDistributedCache> logger) : HybridCache
{
   private readonly CacheConfigurationOptions _config = options.Value;
   private readonly IRedisDatabase _redisDatabase = redisClient.GetDefaultDatabase();

   public override async ValueTask<T> GetOrCreateAsync<TState, T>(string key,
      TState state,
      Func<TState, CancellationToken, ValueTask<T>> factory,
      HybridCacheEntryOptions? options = null,
      IEnumerable<string>? tags = null,
      CancellationToken cancellationToken = default)
   {
      if (options?.Flags is not null)
      {
         logger.LogWarning("HybridCacheEntryFlags are not implemented by Pandatech.DistributedCache.");
      }

      var prefixedKey = CacheKeyFormatter.BuildPrefixedKey(key, _config);

      var lockValue = Guid.NewGuid()
                          .ToString();


      while (true)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var isLocked = await distributedLockService.HasLockAsync(prefixedKey);

         if (isLocked)
         {
            await distributedLockService.WaitUntilLockIsReleasedAsync(prefixedKey, cancellationToken);
            continue;
         }

         var cacheStore = await _redisDatabase.GetAsync<CacheStore<T>>(prefixedKey);
         if (cacheStore is not null)
         {
            if (cacheStore.Tags.Count is 0)
            {
               return cacheStore.Data;
            }

            var cacheInvalidated = false;

            foreach (var tagKey in cacheStore.Tags.Select(tag => CacheKeyFormatter.BuildTagKey(tag, _config)))
            {
               var tagStore = await _redisDatabase.GetAsync<TagStore>(tagKey);

               if (tagStore is null || tagStore.CreatedAt <= cacheStore.CreatedAt)
               {
                  continue;
               }

               await _redisDatabase.RemoveAsync(prefixedKey);
               cacheInvalidated = true;
               break;
            }

            if (cacheInvalidated)
            {
               continue;
            }


            return cacheStore.Data;
         }

         var lockAcquired = await distributedLockService.AcquireLockAsync(prefixedKey, lockValue);

         if (!lockAcquired)
         {
            await distributedLockService.WaitUntilLockIsReleasedAsync(prefixedKey, cancellationToken);
            continue;
         }

         break;
      }

      try
      {
         var value = await factory(state, cancellationToken);
         await SetAsync(key, value, options, tags, cancellationToken);
         return value;
      }
      finally
      {
         await distributedLockService.ReleaseLockAsync(prefixedKey, lockValue);
      }
   }


   public override async ValueTask SetAsync<T>(string key,
      T value,
      HybridCacheEntryOptions? options = null,
      IEnumerable<string>? tags = null,
      CancellationToken cancellationToken = default)
   {
      if (options != null && options.Flags != null &&
          (options.Flags & (HybridCacheEntryFlags.DisableDistributedCache |
                            HybridCacheEntryFlags.DisableLocalCache |
                            HybridCacheEntryFlags.DisableDistributedCacheWrite |
                            HybridCacheEntryFlags.DisableLocalCacheWrite)) != 0)
      {
         return;
      }

      var prefixedKey = CacheKeyFormatter.BuildPrefixedKey(key, _config);
      var expirationTime = options?.Expiration ?? _config.DefaultExpiration;
      var cacheStore = new CacheStore<T>
      {
         Data = value,
         Tags = (tags ?? Array.Empty<string>()).ToList()
      };
      await _redisDatabase.AddAsync(prefixedKey, cacheStore, expirationTime);
   }

   public override async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
   {
      var prefixedKey = CacheKeyFormatter.BuildPrefixedKey(key, _config);
      await _redisDatabase.RemoveAsync(prefixedKey);
   }

   public override async ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
   {
      var tagKey = CacheKeyFormatter.BuildTagKey(tag, _config);
      await _redisDatabase.AddAsync(tagKey, new TagStore());
   }
}