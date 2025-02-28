using System.Diagnostics;
using DistributedCache.Helpers;
using DistributedCache.Options;
using DistributedCache.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace DistributedCache.Services.Implementations;

internal sealed class RedisLockService(IRedisClient redisClient, IOptions<CacheConfigurationOptions> options)
   : IDistributedLockService
{
   private readonly TimeSpan _lockExpiry = options.Value.DistributedLockMaxDuration;
   private readonly TimeSpan _lockRetryDelay = TimeSpan.FromMilliseconds(10);
   private readonly IRedisDatabase _redisDatabase = redisClient.GetDefaultDatabase();
   private readonly TimeSpan _timeout = 2 * options.Value.DistributedLockMaxDuration;

   public Task<bool> AcquireLockAsync(string key, string lockValue)
   {
      var lockKey = CacheKeyFormatter.BuildLockKey(key);
      return _redisDatabase.Database.StringSetAsync(lockKey, lockValue, _lockExpiry, When.NotExists);
   }

   public Task<bool> HasLockAsync(string key)
   {
      var lockKey = CacheKeyFormatter.BuildLockKey(key);

      return _redisDatabase.Database.KeyExistsAsync(lockKey);
   }

   public async Task WaitUntilLockIsReleasedAsync(string key, CancellationToken token)
   {
      var lockKey = CacheKeyFormatter.BuildLockKey(key);
      var stopwatch = Stopwatch.GetTimestamp();

      while (await _redisDatabase.Database.KeyExistsAsync(lockKey) && !token.IsCancellationRequested)
      {
         await Task.Delay(_lockRetryDelay, token);

         var elapsed = Stopwatch.GetElapsedTime(stopwatch);

         if (elapsed > _timeout)
         {
            throw new TimeoutException($"Lock for key {key} was not released within {_timeout}");
         }
      }
   }

   public Task ReleaseLockAsync(string key, string lockValue)
   {
      var lockKey = CacheKeyFormatter.BuildLockKey(key);
      const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

      return _redisDatabase.Database.ScriptEvaluateAsync(script, [lockKey], [lockValue]);
   }
}