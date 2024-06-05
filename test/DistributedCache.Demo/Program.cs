using CacheService.Demo;
using DistributedCache.Extensions;
using DistributedCache.Options;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<CacheTestsService>();

builder.AddCacheService(o =>
{
    o.RedisConnectionString = "localhost:6379";
    o.KeyPrefixForIsolation = KeyPrefix.AssemblyNamePrefix;
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.AddEndpoints();
app.Run();