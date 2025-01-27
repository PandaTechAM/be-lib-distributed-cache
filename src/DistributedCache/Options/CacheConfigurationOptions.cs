namespace DistributedCache.Options;

public class CacheConfigurationOptions
{
   public required string RedisConnectionString { get; set; } = null!;
   public string? ChannelPrefix { get; set; }
   public int ConnectRetry { get; set; } = 10;
   public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
   public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromSeconds(5);
   public TimeSpan DistributedLockMaxDuration { get; set; } = TimeSpan.FromSeconds(8);
   public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);
}