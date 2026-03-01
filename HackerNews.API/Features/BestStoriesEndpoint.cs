namespace HackerNews.Features;

public static class BestStoriesEndpoint
{
    public static WebApplication MapBestStoriesEndpoint(WebApplication app)
    {
        app.MapGet("/api/best-stories", Handle)
            .WithName("GetBestStoriesEndpoint")
            .WithTags("BestStories");

        return app;
    }

    private static async Task<IResult> Handle(int n)
    {
        
        
        return Results.Ok();
    } 
}