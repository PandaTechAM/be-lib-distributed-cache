namespace DistributedCache.Models;

/// <summary>Defines the rules for a rate-limit check: identifiers, attempt cap, and window length.</summary>
public class RateLimitConfiguration : RateLimitKey
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _timeToLive;

    /// <summary>Maximum attempts allowed within the window. Must be greater than zero.</summary>
    public int MaxAttempts
    {
        get => _maxAttempts;
        init =>
            _maxAttempts = value > 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(MaxAttempts), "Must be greater than zero.");
    }

    /// <summary>Duration of the rate-limit window. Must be positive.</summary>
    public TimeSpan TimeToLive
    {
        get => _timeToLive;
        init =>
            _timeToLive = value > TimeSpan.Zero
                ? value
                : throw new ArgumentOutOfRangeException(nameof(TimeToLive), "Must be a positive time span.");
    }

    /// <summary>Set the primary and optional secondary identifiers that scope this rate limit.</summary>
    public override RateLimitConfiguration SetIdentifiers(string primaryIdentifier, string? secondaryIdentifier = null)
    {
        PrimaryIdentifier = primaryIdentifier;
        SecondaryIdentifier = secondaryIdentifier;
        return this;
    }
}
