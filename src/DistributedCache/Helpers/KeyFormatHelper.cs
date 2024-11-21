namespace DistributedCache.Helpers;

internal static class KeyFormatHelper
{
   internal static string GetPrefixedKey(string key, string moduleName)
   {
      return $"{moduleName}:{key}";
   }

   internal static string GetPrefixedKey(string key)
   {
      return key;
   }

   internal static IEnumerable<string> GetPrefixedKeys(IEnumerable<string> keys, string moduleName)
   {
      return keys.Select(key => GetPrefixedKey(key, moduleName));
   }

   internal static IEnumerable<string> GetPrefixedKeys(IEnumerable<string> keys)
   {
      return keys.Select(GetPrefixedKey);
   }

   internal static string GetTagKey(string tag)
   {
      return $"tags:{tag}";
   }

   internal static string GetTagKey(string tag, string moduleName)
   {
      return $"tags:{moduleName}:{tag}";
   }

   internal static string GetLockKey(string key)
   {
      return $"{key}:lock";
   }

   private static string GetTagKey(string tag, string moduleName, object somethingForNoConflict) //Discontinued feature
   {
      return tag == CacheTag.Frequent ? $"tags:{tag}" : $"tags:{moduleName}:{tag}";
   }
}