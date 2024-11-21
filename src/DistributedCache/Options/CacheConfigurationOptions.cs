namespace DistributedCache.Options;

public class CacheConfigurationOptions
{
   public required string RedisConnectionString { get; set; } = null!;
   public KeyPrefix KeyPrefixForIsolation { get; set; } = KeyPrefix.None;
   public int ConnectRetry { get; set; } = 10;
   public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
   public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromSeconds(5);
   public TimeSpan DistributedLockDuration { get; set; } = TimeSpan.FromSeconds(5);
   public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);

   internal CacheResetMode CacheResetMode { get; set; } =
      CacheResetMode.ResetFrequentTagsAfterHealthCheckFail; //Discontinued feature

   internal TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMilliseconds(100); //Discontinued feature
}