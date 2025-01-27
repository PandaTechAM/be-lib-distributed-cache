using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedCache.Extensions;

internal static class HealthCheckExtension
{
   internal static WebApplicationBuilder AddRedisHealthCheck(this WebApplicationBuilder builder,
      string connectionString)
   {
      builder.Services
             .AddHealthChecks()
             .AddRedis(connectionString, timeout: TimeSpan.FromSeconds(3));

      return builder;
   }
}