using System.Reflection;

namespace DistributedCache.Extensions;

public static class StringExtensions
{

   public static string PrefixWithAssemblyName(this string value)
   {
      var assembly = Assembly.GetCallingAssembly();
      return value.PrefixWith(assembly.GetName()
                                      .Name!);
   }

   public static List<string> PrefixWithAssemblyName(this IEnumerable<string> values)
   {
      var assembly = Assembly.GetCallingAssembly().GetName().Name;
      return values.Select(x => x.PrefixWith(assembly!))
                   .ToList();
   }

   public static string PrefixWith(this string value, string prefix)
   {
      return $"{prefix}:{value}";
   }
   public static List<string> PrefixWith(this IEnumerable<string> values, string prefix)
   {
      return values.Select(x => x.PrefixWith(prefix))
                   .ToList();
   }
}