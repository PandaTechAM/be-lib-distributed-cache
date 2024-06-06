namespace DistributedCache.Helpers;

internal static class KeyFormatHelper
{
    internal static string GetPrefixedKey(string key, string moduleName) => $"{moduleName}:{key}";
    internal static string GetPrefixedKey(string key) => key;

    internal static IEnumerable<string> GetPrefixedKeys(IEnumerable<string> keys, string moduleName) =>
        keys.Select(key => GetPrefixedKey(key, moduleName));

    internal static IEnumerable<string> GetPrefixedKeys(IEnumerable<string> keys) => keys.Select(GetPrefixedKey);

    internal static string GetTagKey(string tag) => $"tags:{tag}";
    
    internal static string GetTagKey(string tag, string moduleName) => $"tags:{moduleName}:{tag}";

    internal static string GetLockKey(string key) => $"{key}:lock";
    private static string GetTagKey(string tag, string moduleName, object somethingForNoConflict) //Discontinued feature
    {
        return tag == CacheTag.Frequent ? $"tags:{tag}" : $"tags:{moduleName}:{tag}";
    }
}