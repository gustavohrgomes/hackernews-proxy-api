using HackerNews.ExceptionHandlers;
using HackerNews.Features;
using HackerNews.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

builder.Services
    .AddMemoryCache()
    .AddOutputCache(options =>
    {
        options.AddBasePolicy(build => build.Expire(TimeSpan.FromSeconds(15)));
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
