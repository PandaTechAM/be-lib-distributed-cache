using DistributedCache.Dtos;

namespace DistributedCache.Services.Interfaces;

public interface IRateLimitService
{
    ValueTask<RateLimitState> RateLimitAsync(RateLimitConfiguration rateLimitConfiguration,
        CancellationToken cancellationToken = default);
}