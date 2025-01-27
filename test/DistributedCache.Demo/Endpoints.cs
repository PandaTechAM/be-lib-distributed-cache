using CacheService.Demo.TestCache;
using CacheService.Demo.TestRateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace CacheService.Demo;

public static class Endpoints
{
   public static WebApplication AddEndpoints(this WebApplication app)
   {
      app.MapGet("/get-from-cache",
         async (CacheTestsService service) =>
         {
            await service.GetFromCache();
            return TypedResults.Ok();
         });

      app.MapGet("/get-from-cache-2",
         async (CacheTestsService service) =>
         {
            await service.GetFromCache();
            return TypedResults.Ok();
         });

      app.MapGet("/test-existence",
         async (CacheTestsService service) =>
         {
            await service.TestExistence();
            return TypedResults.Ok();
         });

      app.MapGet("/delete-cache",
         async (CacheTestsService service) =>
         {
            await service.DeleteCache();
            return TypedResults.Ok();
         });

      app.MapPost("/send-sms",
         async ([FromServices] SendSmsService service, CancellationToken token) =>
         {
            var result = await service.SendSms(token);
            return TypedResults.Ok(result);
         });

      return app;
   }
}