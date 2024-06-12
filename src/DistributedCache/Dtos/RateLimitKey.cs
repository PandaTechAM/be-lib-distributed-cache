namespace DistributedCache.Dtos;

public abstract class RateLimitKey
{
    public required int ActionType { get; init; }
    protected string PrimaryIdentifier { get; set; } = null!;
    protected string? SecondaryIdentifier { get; set; }
    
    internal string GetKey()
    {
        return !string.IsNullOrWhiteSpace(SecondaryIdentifier)
            ? $"{ActionType}:{PrimaryIdentifier}:{SecondaryIdentifier}:limit"
            : $"{ActionType}:{PrimaryIdentifier}:limit";
    }

    public abstract RateLimitConfiguration SetIdentifiers(string primaryIdentifier, string? secondaryIdentifier = null);
}