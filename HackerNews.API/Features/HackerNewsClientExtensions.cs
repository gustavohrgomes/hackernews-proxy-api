using Microsoft.Extensions.Options;

namespace HackerNews.Features;

public static class HackerNewsClientExtensions
{
    public static IServiceCollection AddHackerNewsClient(this IServiceCollection services)
    {
        services.AddHttpClient("HackerNewsApi", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HackerNewsApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddScoped<HackerNewsClient>();

        return services;
    }
}