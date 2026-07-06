namespace DistributedCache.Enums;

/// <summary>Result of a rate-limit check.</summary>
public enum RateLimitStatus
{
    /// <summary>The rate limit has been reached; the action should be blocked.</summary>
    Exceeded = 1,

    /// <summary>The rate limit has not been reached; the action is allowed.</summary>
    NotExceeded = 2
}
