using DistributedCache.Enums;
using DistributedCache.Helpers;
using DistributedCache.Models;
using DistributedCache.Options;
using DistributedCache.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace DistributedCache.Services.Implementations;

public class RedisRateLimitService(
   IRedisClient redisClient,
   IOptions<CacheConfigurationOptions> options,
   IDistributedLockService lockService) : IRateLimitService
{
   private readonly CacheConfigurationOptions _config = options.Value;


   private readonly IRedisDatabase _redisDatabase = redisClient.GetDefaultDatabase();

   public async ValueTask<RateLimitState> RateLimitAsync(RateLimitConfiguration rateLimitConfiguration,
      CancellationToken cancellationToken = default)
   {
      var key = CacheKeyFormatter.BuildPrefixedKey(rateLimitConfiguration.GetKey(), _config);

      var lockValue = Guid.NewGuid()
                          .ToString();

      while (true)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var isLocked = await lockService.HasLockAsync(key);

         if (isLocked)
         {
            await lockService.WaitUntilLockIsReleasedAsync(key, cancellationToken);
            continue;
         }

         var lockAcquired = await lockService.AcquireLockAsync(key, lockValue);
         if (!lockAcquired)
         {
            await lockService.WaitUntilLockIsReleasedAsync(key, cancellationToken);
            continue;
         }

         break;
      }

      try
      {
         var cache = await _redisDatabase.GetAsync<RateLimitCache>(key);

         if (cache is null)
         {
            var newCache = RateLimitCache.CreateRateLimitCache(rateLimitConfiguration);
            await _redisDatabase.AddAsync(key, newCache, rateLimitConfiguration.TimeToLive);

            return new RateLimitState(
               RateLimitStatus.NotExceeded,
               rateLimitConfiguration.TimeToLive,
               rateLimitConfiguration.MaxAttempts - 1
            );
         }

         var isUpdated = cache.TryUpdateAttempts();
         var newExpiration = cache.GetNewExpiration();

         if (!isUpdated)
         {
            return new RateLimitState(
               RateLimitStatus.Exceeded,
               newExpiration,
               0
            );
         }

         await _redisDatabase.AddAsync(key, cache, newExpiration);

         return new RateLimitState(
            RateLimitStatus.NotExceeded,
            newExpiration,
            cache.MaxAttempts - cache.Attempts
         );
      }
      finally
      {
         await lockService.ReleaseLockAsync(key, lockValue);
      }
   }
}