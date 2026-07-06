namespace DistributedCache.Options;

/// <summary>Options for configuring the distributed cache and its Redis connection.</summary>
public class CacheConfigurationOptions
{
    /// <summary>Redis connection string in StackExchange.Redis format. Required.</summary>
    public required string RedisConnectionString { get; set; }

    /// <summary>Optional prefix applied to the Redis pub/sub channel used for cache invalidation.</summary>
    public string? ChannelPrefix { get; set; }

    /// <summary>Number of connection retry attempts before failing. Defaults to 10.</summary>
    public int ConnectRetry { get; set; } = 10;

    /// <summary>Timeout for establishing the Redis connection. Defaults to 10 seconds.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Timeout for synchronous Redis operations. Defaults to 5 seconds.</summary>
    public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Maximum time a distributed lock is held before it auto-expires. Defaults to 8 seconds.</summary>
    public TimeSpan DistributedLockMaxDuration { get; set; } = TimeSpan.FromSeconds(8);

    /// <summary>Default entry expiration used when none is specified. Defaults to 15 minutes.</summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);
}
