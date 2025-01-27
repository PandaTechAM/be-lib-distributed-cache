using MessagePack;

namespace DistributedCache.Models;

[MessagePackObject]
public class TagStore
{
   [Key(0)]
   public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}