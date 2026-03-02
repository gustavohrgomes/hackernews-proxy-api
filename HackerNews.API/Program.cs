using HackerNews.ExceptionHandlers;
using HackerNews.Features;
using HackerNews.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddOptionsWithValidateOnStart<HackerNewsApiOptions>()
    .BindConfiguration(HackerNewsApiOptions.SectionName)
    .ValidateDataAnnotations();

builder.Services
    .AddOptionsWithValidateOnStart<CacheOptions>()
    .BindConfiguration(CacheOptions.SectionName)
    .ValidateDataAnnotations();

var cacheOptions = builder.Configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

builder.Services
    .AddMemoryCache()
    .AddOutputCache(options =>
    {
        options.AddBasePolicy(build => build.Expire(TimeSpan.FromSeconds(cacheOptions.OutputCacheTtlSeconds)));
    })
    .AddHackerNewsClient()
    .AddScoped<BestStoriesHandler>()
    .AddExceptionHandler<HackerNewsExceptionHandler>()
    .AddProblemDetails();

var app = builder.Build();

app
    .MapDefaultEndpoints()
    .MapBestStoriesEndpoint();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseOutputCache();

app.Run();
