using DistributedCache.Options;

namespace DistributedCache.Helpers;

internal static class CacheKeyFormatter
{
   private const string DefaultCachePrefix = "DistributedCache";

   internal static string BuildPrefixedKey(string key,
      CacheConfigurationOptions options)
   {
      return options.ChannelPrefix is not null
         ? $"{DefaultCachePrefix}:{options.ChannelPrefix}:{key}"
         : $"{DefaultCachePrefix}:{key}";
   }

   internal static IEnumerable<string> BuildPrefixedKeys(IEnumerable<string> keys,
      CacheConfigurationOptions options)
   {
      return keys.Select(k => BuildPrefixedKey(k, options));
   }

   internal static string BuildTagKey(string tagName,
      CacheConfigurationOptions options)
   {
      return options.ChannelPrefix is not null
         ? $"{DefaultCachePrefix}:{options.ChannelPrefix}:tag:{tagName}"
         : $"{DefaultCachePrefix}:tag:{tagName}";
   }

   internal static string BuildLockKey(string baseKey)
   {
      return $"{baseKey}:lock";
   }
}