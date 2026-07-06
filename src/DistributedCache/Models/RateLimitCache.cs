using MessagePack;

namespace DistributedCache.Models;

/// <summary>Redis-stored counter tracking attempts against a rate-limit window.</summary>
[MessagePackObject]
public class RateLimitCache
{
    /// <summary>Number of attempts recorded so far. Starts at 1.</summary>
    [Key(0)]
    public int Attempts { get; set; } = 1;

    /// <summary>Maximum attempts allowed within the window.</summary>
    [Key(1)]
    public int MaxAttempts { get; init; }

    /// <summary>UTC time when the window expires.</summary>
    [Key(2)]
    public DateTime Expiration { get; init; }

    /// <summary>Create a new counter from the given rate-limit configuration.</summary>
    public static RateLimitCache CreateRateLimitCache(RateLimitConfiguration configuration)
    {
        return new RateLimitCache
        {
            MaxAttempts = configuration.MaxAttempts,
            Expiration = DateTime.UtcNow + configuration.TimeToLive
        };
    }

    internal bool TryUpdateAttempts()
    {
        if (Attempts >= MaxAttempts)
        {
            return false;
        }

        Attempts++;
        return true;
    }

    internal TimeSpan GetNewExpiration()
    {
        return Expiration - DateTime.UtcNow;
    }
}
