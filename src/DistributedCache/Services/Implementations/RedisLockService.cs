using DistributedCache.Helpers;
using DistributedCache.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace DistributedCache.Services.Implementations;

public class RedisLockService(IRedisClient redisClient, IOptions<CacheConfigurationOptions> options)
{
   private readonly TimeSpan _lockExpiry = options.Value.DistributedLockDuration;
   private readonly TimeSpan _lockRetryDelay = TimeSpan.FromMilliseconds(10);
   private readonly IRedisDatabase _redisDatabase = redisClient.GetDefaultDatabase();

   public async Task<bool> AcquireLockAsync(string key, string lockValue)
   {
      var lockKey = KeyFormatHelper.GetLockKey(key);
      return await _redisDatabase.Database.StringSetAsync(lockKey, lockValue, _lockExpiry, When.NotExists);
   }

   public async Task<bool> CheckForLockAsync(string key)
   {
      var lockKey = KeyFormatHelper.GetLockKey(key);
      return await _redisDatabase.Database.KeyExistsAsync(lockKey);
   }

   public async Task WaitForLockReleaseAsync(string key, CancellationToken token)
   {
      var lockKey = KeyFormatHelper.GetLockKey(key);
      while (await _redisDatabase.Database.KeyExistsAsync(lockKey) && !token.IsCancellationRequested)
      {
         await Task.Delay(_lockRetryDelay, token);
      }
   }

   public async Task ReleaseLockAsync(string key, string lockValue)
   {
      var lockKey = KeyFormatHelper.GetLockKey(key);

      const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

      await _redisDatabase.Database.ScriptEvaluateAsync(script, [lockKey], [lockValue]);
   }
}