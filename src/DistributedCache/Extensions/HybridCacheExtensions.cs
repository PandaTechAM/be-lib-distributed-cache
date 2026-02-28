using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Hybrid;

namespace DistributedCache.Extensions;

public static class HybridCacheExtensions
{
   private static readonly HybridCacheEntryOptions ReadOnlyOptions = new()
   {
      Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
   };

   public static async ValueTask<TValue> GetOrDefaultAsync<TValue>(this HybridCache cache,
      string key,
      TValue defaultValue,
      CancellationToken ct = default)
   {
      return await cache.GetOrCreateAsync<TValue, TValue>(
         key,
         defaultValue,
         static (value, _) => new ValueTask<TValue>(value),
         ReadOnlyOptions,
         cancellationToken: ct);
   }

   public static async ValueTask<(bool Exists, TValue? Value)> TryGetAsync<TValue>(this HybridCache cache,
      string key,
      CancellationToken ct = default)
   {
      // Factory only runs on a cache miss. Start as true; factory sets false when key is absent.
      var found = new StrongBox<bool>(true);

      var value = await cache.GetOrCreateAsync<StrongBox<bool>, TValue>(
         key,
         found,
         static (state, _) =>
         {
            state.Value = false;
            return new ValueTask<TValue>(default(TValue)!);
         },
         ReadOnlyOptions,
         cancellationToken: ct);

      return (found.Value, found.Value ? value : default);
   }

   public static async ValueTask<bool> ExistsAsync<TValue>(this HybridCache cache,
      string key,
      CancellationToken ct = default)
   {
      var (exists, _) = await cache.TryGetAsync<TValue>(key, ct);
      return exists;
   }
}