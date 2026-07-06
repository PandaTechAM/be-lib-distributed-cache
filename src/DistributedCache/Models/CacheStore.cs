using DistributedCache.Services.Interfaces;
using MessagePack;

namespace DistributedCache.Models;

/// <summary>Envelope stored in Redis for a cached value, carrying its tags and creation time.</summary>
[MessagePackObject]
public class CacheStore<T> : ICacheEntity
{
    /// <summary>The cached value.</summary>
    [Key(0)]
    public required T Data { get; set; }

    /// <summary>UTC timestamp when this entry was created.</summary>
    [Key(1)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Tags associated with this entry, used for grouped invalidation.</summary>
    [Key(2)]
    public required List<string> Tags { get; set; }
}
