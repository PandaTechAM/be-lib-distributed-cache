using DistributedCache.Enums;

namespace DistributedCache.Dtos;

public record RateLimitState(RateLimitStatus Status, TimeSpan TimeToReset, int RemainingAttempts);