using DistributedCache.Dtos;

namespace CacheService.Demo.TestRateLimiting;

public static class RateLimitingConfigurations
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