namespace CacheService.Options;

public class CacheConfigurationOptions
{
    public required string RedisConnectionString { get; set; } = null!;
    public KeyPrefix KeyPrefixForIsolation { get; set; } = KeyPrefix.None;
    public int ConnectRetry { get; set; } = 10;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan DistributedLockDuration { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public CacheResetMode CacheResetMode { get; set; } = CacheResetMode.ResetFrequentTagsAfterHealthCheckFail;
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}

public enum CacheResetMode
{
    None = 1,
    ResetFrequentTagsAfterHealthCheckFail = 2
}