using System.Reflection;

namespace DistributedCache.Extensions;

public static class StringExtensions
{
   public static string PrefixWith(this string value, string prefix)
   {
      return $"{prefix}{value}";
   }

   public static string PrefixWith(this string value, Assembly assembly)
   {
      return $"{assembly.GetName().Name}:{value}";
   }

   public static List<string> PrefixWith(this IEnumerable<string> values, string prefix)
   {
      return values.Select(x => x.PrefixWith(prefix))
                   .ToList();
   }

   public static List<string> PrefixWith(this IEnumerable<string> values, Assembly assembly)
   {
      return values.Select(x => x.PrefixWith(assembly))
                   .ToList();
   }
}