using DistributedCache.Helpers;
using DistributedCache.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace DistributedCache.Services.Implementations;

public class RedisHealthCheckService(
    IRedisClient redisClient,
    ILogger<RedisHealthCheckService> logger,
    IOptions<CacheConfigurationOptions> options) // Discontinued Feature
    : BackgroundService
{
    private readonly IRedisDatabase _redisDatabase = redisClient.GetDefaultDatabase();
    private readonly CacheConfigurationOptions _config = options.Value;
    private bool _resetRedis;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_config.CacheResetMode == CacheResetMode.None)
        {
            logger.LogWarning(
                "Cache reset mode is set to None. RedisHealthCheckService will not run. This might risk cache inconsistency after Redis unavailability.");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        logger.LogInformation("RedisHealthCheckService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_config.HealthCheckInterval, stoppingToken);
            var healthyRedis = await CheckRedisHealthAsync();

            if (healthyRedis)
            {
                if (!_resetRedis) continue;

                var redisResetSuccess = await RemoveByFrequentTagAsync();
                if (redisResetSuccess)
                {
                    _resetRedis = false;
                }
            }
            else
            {
                _resetRedis = true;
            }
        }
    }

    private async Task<bool> CheckRedisHealthAsync()
    {
        try
        {
            await _redisDatabase.Database.PingAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis ping failed.");
            return false;
        }
    }

    private async Task<bool> RemoveByFrequentTagAsync()
    {
        var key = KeyFormatHelper.GetTagKey(CacheTag.Frequent);
        try
        {
            var keys = await _redisDatabase.SetMembersAsync<string>(key);

            if (keys.Length > 0)
            {
                await _redisDatabase.RemoveAllAsync(keys);
            }

            await _redisDatabase.RemoveAsync(key);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemoveByTagAsync failed.");
            return false;
        }
    }
}