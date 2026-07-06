namespace DistributedCache.Models;

/// <summary>Base key for a rate limit: an action type plus one or two identifiers.</summary>
public abstract class RateLimitKey
{
    /// <summary>Identifier of the action being rate limited.</summary>
    public required int ActionType { get; init; }

    /// <summary>Primary identifier (e.g. user or IP) that scopes the limit.</summary>
    protected string PrimaryIdentifier { get; set; } = null!;

    /// <summary>Optional secondary identifier that further scopes the limit.</summary>
    protected string? SecondaryIdentifier { get; set; }

    internal string GetKey()
    {
        return !string.IsNullOrWhiteSpace(SecondaryIdentifier)
            ? $"{ActionType}:{PrimaryIdentifier}:{SecondaryIdentifier}:limit"
            : $"{ActionType}:{PrimaryIdentifier}:limit";
    }

    /// <summary>Set the primary and optional secondary identifiers that scope this rate limit.</summary>
    public abstract RateLimitConfiguration SetIdentifiers(string primaryIdentifier, string? secondaryIdentifier = null);
}
