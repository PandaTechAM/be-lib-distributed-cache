using DistributedCache.Extensions;
using Microsoft.Extensions.Caching.Hybrid;

namespace CacheService.Demo.TestCache;

public class CacheTestsService(HybridCache hybridCache)
{
   public async Task GetFromCache(CancellationToken token = default)
   {
      var call1 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5)
         },
         ["test"],
         token);


      var call2 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5)
         },
         ["test"],
         token);

      var call3 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test2",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5)
         },
         ["vazgen"],
         token);


      var call4 = await hybridCache.GetOrCreateAsync<TestCacheEntity>("test3",
         async _ => await GetFromPostgres(token),
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.FromMinutes(5)
         },
         ["test", "vazgen"],
         token);
   }

   public async Task TestExistence(CancellationToken token = default)
   {
      var call1Check = await hybridCache.ExistsAsync<TestCacheEntity>("test", token);
      Console.WriteLine($"Call1: {call1Check}");
      var call2Check = await hybridCache.ExistsAsync<TestCacheEntity>("test", token);
      Console.WriteLine($"Call2: {call2Check}");
      var call3Check = await hybridCache.ExistsAsync<TestCacheEntity>("test2", token);
      Console.WriteLine($"Call3: {call3Check}");
      var call4Check = await hybridCache.ExistsAsync<TestCacheEntity>("test3", token);
      Console.WriteLine($"Call4: {call4Check}");
   }

   public async Task DeleteCache(CancellationToken token = default)
   {
      await hybridCache.RemoveByTagAsync("test", token);
   }

   public async Task<TestCacheEntity> GetFromPostgres(CancellationToken token)
   {
      Console.WriteLine("Hey, I'm Fetching from postgres");
      await Task.Delay(500, token);
      return new TestCacheEntity();
   }
}