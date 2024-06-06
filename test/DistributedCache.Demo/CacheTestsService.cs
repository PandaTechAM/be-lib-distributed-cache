using DistributedCache.Helpers;
using DistributedCache.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;

namespace CacheService.Demo;

public class CacheTestsService(ICacheService<TestCacheEntity> cacheService)
{
    public async Task GetFromCache(CancellationToken token = default)
    {
        await cacheService.GetOrCreateAsync("test",
            async _ => await GetFromPostgres(token),
            TimeSpan.FromMinutes(1),
            ["test"],
            token);

        await cacheService.GetOrCreateAsync("test2",
           async _ => await GetFromPostgres(token),
           TimeSpan.FromMinutes(1),
           ["vazgen"],
           token);
        
        await cacheService.GetOrCreateAsync("test3",
           async _ => await GetFromPostgres(token),
           TimeSpan.FromMinutes(1),
           ["test", "vazgen"],
           token);
         
    }

    public async Task DeleteCache(CancellationToken token = default)
    {
        await cacheService.RemoveByTagAsync("test", token);
    }
    public async Task<TestCacheEntity> GetFromPostgres(CancellationToken token)
    {
        Console.WriteLine("Hey, I'm Fetching from postgres");
        await Task.Delay(500, token);
        return new TestCacheEntity();
    }
}