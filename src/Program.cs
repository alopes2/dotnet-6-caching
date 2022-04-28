using System.Text.Json;
using Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

var redisOptions = builder.Configuration.GetSection(RedisOptions.Name).Get<RedisOptions>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisOptions.ConnectionString;
    options.InstanceName = redisOptions.InstanceName;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/in-memory-cache", async(IMemoryCache memoryCache) =>
    {
        return await memoryCache.GetOrCreateAsync("InMemory", async cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);

            await Task.Delay(2000);

            var timeNow = TimeOnly.FromDateTime(DateTime.Now);
            var responseData = "Hello World! The time is: " + timeNow.ToLongTimeString();

            return new Response(responseData);
        });
    })
    .WithName("InMemoryCache");

app.MapGet("/distributed-cache", async(IDistributedCache distributedCache) =>
    {
        var cachedValue = await distributedCache.GetStringAsync("DistributedCache");

        // If there's a value, we just return it as the response
        if (cachedValue is not null)
        {
            var cachedResponse = JsonSerializer.Deserialize<Response>(cachedValue);
            return cachedResponse;
        }

        await Task.Delay(2000);

        var timeNow = TimeOnly.FromDateTime(DateTime.Now);
        var responseData = "Hello World! The time is: " + timeNow.ToLongTimeString();

        var response = new Response(responseData);

        var cacheEntryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
        };

        // Serialize our model so we can store it
        var jsonResponse = JsonSerializer.Serialize(response);

        await distributedCache.SetStringAsync("DistributedCache", jsonResponse, cacheEntryOptions);

        return response;
    })
    .WithName("DistributedCache");

app.Run();