using MessagePack;

namespace DistributedCache.Models;

/// <summary>Marker entry stored in Redis to track a tag and its creation time.</summary>
[MessagePackObject]
public class TagStore
{
    /// <summary>UTC timestamp when this tag entry was created.</summary>
    [Key(0)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
