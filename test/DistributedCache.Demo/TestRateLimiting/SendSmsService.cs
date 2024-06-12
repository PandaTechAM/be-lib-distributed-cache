using DistributedCache.Dtos;
using DistributedCache.Services.Interfaces;

namespace CacheService.Demo.TestRateLimiting;

public class SendSmsService(IRateLimitService rateLimitService)
{
    public async Task<RateLimitState> SendSms(CancellationToken cancellationToken = default)
    {
        var phoneNumber = "1234567890";
        var rateLimitConfiguration = RateLimitingConfigurations.GetSmsConfig().SetIdentifiers(phoneNumber);

        return await rateLimitService.RateLimitAsync(rateLimitConfiguration, cancellationToken);
    }
}