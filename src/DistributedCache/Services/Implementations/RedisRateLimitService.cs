using DistributedCache.Dtos;
using DistributedCache.Enums;
using DistributedCache.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DistributedCache.Services.Implementations;

public class RedisRateLimitService(ICacheService<RateLimitCache> cacheService, RedisLockService lockService)
    : IRateLimitService
{
    public async ValueTask<RateLimitState> RateLimitAsync(RateLimitConfiguration rateLimitConfiguration,
        CancellationToken cancellationToken = default)
    {
        var key = rateLimitConfiguration.GetKey();
      
        var cache = await cacheService.GetAsync(key, cancellationToken);

        if (cache == null)
        {
            var newCache = RateLimitCache.CreateRateLimitCache(rateLimitConfiguration);
            await cacheService.SetAsync(key, newCache, rateLimitConfiguration.TimeToLive,null ,cancellationToken);
            
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

        await cacheService.SetAsync(key, cache, newExpiration, null, cancellationToken);

        return new RateLimitState(
            RateLimitStatus.NotExceeded, 
            newExpiration, 
            cache.MaxAttempts - cache.Attempts
        );
    }
}