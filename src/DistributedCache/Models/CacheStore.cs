using DistributedCache.Services.Interfaces;
using MessagePack;

namespace DistributedCache.Models;

[MessagePackObject]
public class CacheStore<T> : ICacheEntity
{
   [Key(0)]
   public required T Data { get; set; }

   [Key(1)]
   public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

   [Key(2)]
   public required List<string> Tags { get; set; }
}