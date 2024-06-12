using DistributedCache.Services.Interfaces;
using MessagePack;

namespace CacheService.Demo;

[MessagePackObject]
public class TestCacheEntity : ICacheEntity //todo why do we need this ICacheEntity
{
    [Key(0)] public string Name { get; set; } = "Bob";

    [Key(1)] public int Age { get; set; } = 15;
    [Key(2)] public DateTime CreatedAt { get; set; } = DateTime.Now;
}