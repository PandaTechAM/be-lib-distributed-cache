using MessagePack;

namespace DistributedCache.Models;

[MessagePackObject]
public class RateLimitCache
{
   [Key(0)]
   public int Attempts { get; set; } = 1;

   [Key(1)]
   public int MaxAttempts { get; init; }

   [Key(2)]
   public DateTime Expiration { get; init; }

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