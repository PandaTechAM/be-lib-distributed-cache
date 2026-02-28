using DistributedCache.Options;
using DistributedCache.Serializers;
using DistributedCache.Services.Implementations;
using DistributedCache.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;

namespace DistributedCache.Extensions;

public static class WebApplicationBuilderExtension
{
   public static WebApplicationBuilder AddDistributedCache(this WebApplicationBuilder builder,
      Action<CacheConfigurationOptions> configureOptions)
   {
      var config = new CacheConfigurationOptions { RedisConnectionString = null! };
      configureOptions(config);
      ValidateOptions(config);

      builder.Services.Configure(configureOptions);

      var redisOptions = ConfigurationOptions.Parse(config.RedisConnectionString);
      redisOptions.ConnectRetry = config.ConnectRetry;
      redisOptions.ConnectTimeout = (int)config.ConnectTimeout.TotalMilliseconds;
      redisOptions.SyncTimeout = (int)config.SyncTimeout.TotalMilliseconds;
      redisOptions.DefaultDatabase = 0;
      redisOptions.ReconnectRetryPolicy = new ExponentialRetry(10000);

      builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));

      builder.Services.AddSingleton<IRateLimitService, RedisRateLimitService>();
      builder.Services.AddSingleton<IDistributedLockService, RedisLockService>();
      builder.Services.AddSingleton<HybridCache, RedisDistributedCache>();

      var redisConfiguration = new RedisConfiguration
      {
         ConnectionString = config.RedisConnectionString,
         Name = "DistributedCacheConfiguration"
      };

      builder.Services.AddStackExchangeRedisExtensions<RedisMsgPackObjectSerializer>(redisConfiguration);

      builder.AddRedisHealthCheck(config.RedisConnectionString);

      return builder;
   }

   private static void ValidateOptions(CacheConfigurationOptions options)
   {
      if (string.IsNullOrEmpty(options.RedisConnectionString))
         throw new ArgumentException("AddDistributedCache: RedisConnectionString is required.");

      if (options.ConnectRetry <= 0)
         throw new ArgumentException("AddDistributedCache: ConnectRetry must be greater than 0.");

      if (options.ConnectTimeout <= TimeSpan.Zero)
         throw new ArgumentException("AddDistributedCache: ConnectTimeout must be greater than 0.");

      if (options.SyncTimeout <= TimeSpan.Zero)
         throw new ArgumentException("AddDistributedCache: SyncTimeout must be greater than 0.");

      if (options.DistributedLockMaxDuration <= TimeSpan.FromSeconds(1))
         throw new ArgumentException("AddDistributedCache: DistributedLockMaxDuration must be greater than 1 second.");

      if (options.DefaultExpiration <= TimeSpan.Zero)
         throw new ArgumentException("AddDistributedCache: DefaultExpiration must be greater than 0.");
   }
}