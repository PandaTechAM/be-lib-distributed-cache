using Microsoft.Extensions.Caching.Hybrid;

namespace DistributedCache.Extensions;

public static class HybridCacheExtensions
{
   public static async ValueTask<TValue> GetOrDefaultAsync<TValue>(this HybridCache cache,
      string key,
      TValue defaultValue,
      CancellationToken ct = default)
   {
      return await cache.GetOrCreateAsync<TValue>(
         key,
         _ => new ValueTask<TValue>(defaultValue),
         new HybridCacheEntryOptions
         {
            Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
         },
         cancellationToken: ct
      );
   }

   public static async ValueTask<(bool Exists, TValue? Value)> TryGetAsync<TValue>(this HybridCache cache,
      string key,
      CancellationToken ct = default)
   {
      var exists = true;
      var value = await cache.GetOrCreateAsync<TValue>(
         key,
         _ =>
         {
            exists = false;
            return new ValueTask<TValue>(default(TValue)!);
         },
         new HybridCacheEntryOptions
         {
            Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
         },
         cancellationToken: ct
      );
      return (exists, value);
   }

   public static async ValueTask<bool> ExistsAsync<TValue>(this HybridCache cache,
      string key,
      CancellationToken ct = default)
   {
      var (exists, _) = await cache.TryGetAsync<TValue>(key, ct);
      return exists;
   }
}