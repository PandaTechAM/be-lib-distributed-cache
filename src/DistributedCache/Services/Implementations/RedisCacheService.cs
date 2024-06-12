using DistributedCache.Dtos;
using DistributedCache.Helpers;
using DistributedCache.Options;
using DistributedCache.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace DistributedCache.Services.Implementations;

internal class RedisCacheService<T>(
    IRedisClient redisClient,
    IOptions<CacheConfigurationOptions> options,
    RedisLockService lockService)
    : ICacheService<T>
    where T : class, ICacheEntity
{
    private readonly IRedisDatabase _redisDatabase = redisClient.GetDefaultDatabase();
    private readonly CacheConfigurationOptions _config = options.Value;
    private readonly string _moduleName = typeof(T).Assembly.GetName().Name!;

    public async ValueTask<T> GetOrCreateAsync(string key, Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        var prefixedKey = _config.KeyPrefixForIsolation == KeyPrefix.None
            ? KeyFormatHelper.GetPrefixedKey(key)
            : KeyFormatHelper.GetPrefixedKey(key, _moduleName);


        var lockValue = Guid.NewGuid().ToString();

        while (true)
        {
            token.ThrowIfCancellationRequested();

            var isLocked = await lockService.CheckForLockAsync(prefixedKey);

            if (isLocked)
            {
                await lockService.WaitForLockReleaseAsync(prefixedKey, token);
                continue;
            }

            var cachedValue = await _redisDatabase.GetAsync<T>(prefixedKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var lockAcquired = await lockService.AcquireLockAsync(prefixedKey, lockValue);

            if (!lockAcquired)
            {
                await lockService.WaitForLockReleaseAsync(prefixedKey, token);
                continue;
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
            await lockService.ReleaseLockAsync(prefixedKey, lockValue);
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
}