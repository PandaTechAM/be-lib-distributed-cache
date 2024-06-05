using DistributedCache.Helpers;
using DistributedCache.Options;
using DistributedCache.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace DistributedCache.Services.Implementations;

internal class RedisCacheService<T>(IRedisClient redisClient, IOptions<CacheConfigurationOptions> options)
    : ICacheService<T>
    where T : class, ICacheEntity
{
    private readonly IRedisDatabase _redisDatabase = redisClient.GetDefaultDatabase();
    private readonly CacheConfigurationOptions _config = options.Value;
    private readonly string _moduleName = typeof(T).Assembly.GetName().Name!;
    private readonly TimeSpan _lockExpiry = options.Value.DistributedLockDuration;
    private readonly TimeSpan _lockRetryDelay = TimeSpan.FromMilliseconds(20);

    public async ValueTask<T> GetOrCreateAsync(string key, Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        var prefixedKey = _config.KeyPrefixForIsolation == KeyPrefix.None
            ? KeyFormatHelper.GetPrefixedKey(key)
            : KeyFormatHelper.GetPrefixedKey(key, _moduleName);

        var lockKey = $"{prefixedKey}:lock";
        var lockValue = Guid.NewGuid().ToString();

        while (true)
        {
            token.ThrowIfCancellationRequested();

            // Acquire lock before getting the value
            var lockAcquired = await AcquireLockAsync(lockKey, lockValue);

            if (!lockAcquired)
            {
                await WaitForLockReleaseAsync(lockKey, token);
                continue;
            }

            var cachedValue = await _redisDatabase.GetAsync<T>(prefixedKey);

            if (cachedValue != null)
            {
                await ReleaseLockAsync(lockKey, lockValue);
                return cachedValue;
            }
            break;
        }

        try
        {
            var value = await factory(token);
            await SetAsync(key, value, expiration, tags, token);
            return value;
        }
        finally
        {
            // Release the lock
            await ReleaseLockAsync(lockKey, lockValue);
        }
    }

    public async ValueTask<T?> GetAsync(string key, CancellationToken token = default)
    {
        var prefixedKey = _config.KeyPrefixForIsolation == KeyPrefix.None
            ? KeyFormatHelper.GetPrefixedKey(key)
            : KeyFormatHelper.GetPrefixedKey(key, _moduleName);

        return await _redisDatabase.GetAsync<T>(prefixedKey);
    }

    public async ValueTask SetAsync(string key, T value, TimeSpan? expiration = null,
        IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        var prefixedKey = _config.KeyPrefixForIsolation == KeyPrefix.None
            ? KeyFormatHelper.GetPrefixedKey(key)
            : KeyFormatHelper.GetPrefixedKey(key, _moduleName);

        var expirationTime = expiration ?? _config.DefaultExpiration;

        await _redisDatabase.AddAsync(prefixedKey, value, expirationTime);

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                var tagKey = _config.KeyPrefixForIsolation == KeyPrefix.None
                    ? KeyFormatHelper.GetTagKey(tag)
                    : KeyFormatHelper.GetTagKey(tag, _moduleName);

                await _redisDatabase.SetAddAsync(tagKey, prefixedKey);
            }
        }
    }

    public async ValueTask RemoveByKeyAsync(string key, CancellationToken token = default)
    {
        var prefixedKey = _config.KeyPrefixForIsolation == KeyPrefix.None
            ? KeyFormatHelper.GetPrefixedKey(key)
            : KeyFormatHelper.GetPrefixedKey(key, _moduleName);

        await _redisDatabase.RemoveAsync(prefixedKey);
    }

    public async ValueTask RemoveByKeysAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        var prefixedKeys = _config.KeyPrefixForIsolation == KeyPrefix.None
            ? KeyFormatHelper.GetPrefixedKeys(keys)
            : KeyFormatHelper.GetPrefixedKeys(keys, _moduleName);

        await _redisDatabase.RemoveAllAsync(prefixedKeys.ToArray());
    }

    public async ValueTask RemoveByTagAsync(string tag, CancellationToken token = default)
    {
        var tagKey = _config.KeyPrefixForIsolation == KeyPrefix.None
            ? KeyFormatHelper.GetTagKey(tag)
            : KeyFormatHelper.GetTagKey(tag, _moduleName);
        
        var keys = await _redisDatabase.SetMembersAsync<string>(tagKey);
        if (keys.Length > 0)
        {
            await _redisDatabase.RemoveAllAsync(keys);
        }

        await _redisDatabase.RemoveAsync(tagKey);
    }

    public async ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken token = default)
    {
        var tasks = tags.Select(tag => RemoveByTagAsync(tag, token).AsTask());
        await Task.WhenAll(tasks);
    }

    private async Task<bool> AcquireLockAsync(string lockKey, string lockValue)
    {
        return await _redisDatabase.Database.StringSetAsync(lockKey, lockValue, _lockExpiry, When.NotExists);
    }

    private async Task WaitForLockReleaseAsync(string lockKey, CancellationToken token)
    {
        while (await _redisDatabase.Database.KeyExistsAsync(lockKey) && !token.IsCancellationRequested)
        {
            await Task.Delay(_lockRetryDelay, token);
        }
    }

    private async Task ReleaseLockAsync(string lockKey, string lockValue)
    {
        // Check if the current instance owns the lock before releasing it
        const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

        await _redisDatabase.Database.ScriptEvaluateAsync(script, [lockKey], [lockValue]);
    }
}