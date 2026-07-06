using System.Reflection;

namespace DistributedCache.Extensions;

/// <summary>Helpers for building namespaced cache keys.</summary>
public static class StringExtensions
{
    /// <summary>Prefix a key with the calling assembly's name (<c>AssemblyName:key</c>).</summary>
    public static string PrefixWithAssemblyName(this string value)
    {
        var assembly = Assembly.GetCallingAssembly();
        return value.PrefixWith(assembly.GetName()
            .Name!);
    }

    /// <summary>Prefix each key with the calling assembly's name.</summary>
    public static List<string> PrefixWithAssemblyName(this IEnumerable<string> values)
    {
        var assembly = Assembly.GetCallingAssembly().GetName().Name;
        return values.Select(x => x.PrefixWith(assembly!))
            .ToList();
    }

    /// <summary>Prefix a key with the given prefix (<c>prefix:key</c>).</summary>
    public static string PrefixWith(this string value, string prefix)
    {
        return $"{prefix}:{value}";
    }

    /// <summary>Prefix each key with the given prefix.</summary>
    public static List<string> PrefixWith(this IEnumerable<string> values, string prefix)
    {
        return values.Select(x => x.PrefixWith(prefix))
            .ToList();
    }
}
