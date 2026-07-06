using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Hybrid;

namespace DistributedCache.Extensions;

/// <summary>Read-only lookup helpers over <c>HybridCache</c> that never write back to the cache.</summary>
public static class HybridCacheExtensions
{
    private static readonly HybridCacheEntryOptions ReadOnlyOptions = new()
    {
        Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
    };

    /// <summary>Return the cached value for a key, or the supplied default if the key is absent, without populating the cache.</summary>
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

    /// <summary>Try to read a value without populating the cache; returns whether the key existed and its value.</summary>
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

    /// <summary>Return whether a key exists in the cache without populating it.</summary>
    public static async ValueTask<bool> ExistsAsync<TValue>(this HybridCache cache,
        string key,
        CancellationToken ct = default)
    {
        var (exists, _) = await cache.TryGetAsync<TValue>(key, ct);
        return exists;
    }
}
