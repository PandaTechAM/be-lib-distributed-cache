using DistributedCache.Enums;

namespace DistributedCache.Models;

/// <summary>Outcome of a rate-limit check.</summary>
/// <param name="Status">Whether the rate limit was exceeded.</param>
/// <param name="TimeToReset">Time remaining until the window resets.</param>
/// <param name="RemainingAttempts">Attempts still allowed within the current window.</param>
public record RateLimitState(RateLimitStatus Status, TimeSpan TimeToReset, int RemainingAttempts);
