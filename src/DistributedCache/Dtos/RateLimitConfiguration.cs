namespace DistributedCache.Dtos;

public class RateLimitConfiguration : RateLimitKey
{
   private readonly int _maxAttempts;
   private readonly TimeSpan _timeToLive;

   public int MaxAttempts
   {
      get => _maxAttempts;
      init =>
         _maxAttempts = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaxAttempts), "Must be greater than zero.");
   }

   public TimeSpan TimeToLive
   {
      get => _timeToLive;
      init =>
         _timeToLive = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(TimeToLive), "Must be a positive time span.");
   }

   public override RateLimitConfiguration SetIdentifiers(string primaryIdentifier, string? secondaryIdentifier = null)
   {
      PrimaryIdentifier = primaryIdentifier;
      SecondaryIdentifier = secondaryIdentifier;
      return this;
   }
}