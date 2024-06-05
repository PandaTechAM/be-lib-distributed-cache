namespace CacheService.Demo;

public static class Endpoints
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        app.MapGet("/get-from-cache", async (CacheTestsService service) =>
        {
            await service.GetFromCache();
            return TypedResults.Ok();
        });

        app.MapGet("/get-from-cache-2", async (CacheTestsService service) =>
        {
            await service.GetFromCache();
            return TypedResults.Ok();
        });

        app.MapGet("/delete-cache", async (CacheTestsService service) =>
        {
            await service.DeleteCache();
            return TypedResults.Ok();
        });

        return app;
    }
}