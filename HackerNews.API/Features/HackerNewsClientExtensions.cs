namespace HackerNews.Features;

public static class HackerNewsClientExtensions
{
    public static IServiceCollection AddHackerNewsClient(this IServiceCollection services)
    {
        services.AddHttpClient("HackerNews", client =>
        {
            client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
        });

        services.AddScoped<HackerNewsClient>();

        return services;
    }
}