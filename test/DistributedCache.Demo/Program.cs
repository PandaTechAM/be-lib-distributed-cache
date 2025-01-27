using CacheService.Demo;
using CacheService.Demo.TestCache;
using CacheService.Demo.TestRateLimiting;
using DistributedCache.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<CacheTestsService>();
builder.Services.AddScoped<SendSmsService>();


builder.AddDistributedCache(o =>
{
   o.RedisConnectionString = "localhost:6379";
   o.ChannelPrefix = "app_name";
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.AddEndpoints();
app.Run();