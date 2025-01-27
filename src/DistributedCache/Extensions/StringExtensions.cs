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
}