using DistributedCache.Enums;

namespace DistributedCache.Models;

public record RateLimitState(RateLimitStatus Status, TimeSpan TimeToReset, int RemainingAttempts);