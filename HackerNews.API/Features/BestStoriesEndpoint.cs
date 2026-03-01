namespace HackerNews.Features;

public static class BestStoriesEndpoint
{
    private const int MinimumBestStories = 1;
    private const int MaximumBestStories = 200;
    
    public static WebApplication MapBestStoriesEndpoint(this WebApplication app)
    {
        app.MapGet("/api/best-stories", Handle)
            .WithName("GetBestStoriesEndpoint")
            .WithTags("BestStories")
            .Produces<IEnumerable<GetBestStoriesResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .CacheOutput();

        return app;
    }

    private static async Task<IResult> Handle(int n, BestStoriesHandler handler, CancellationToken cancellationToken = default)
    {
        if (n is < MinimumBestStories or > MaximumBestStories)
        {
            return Results.Problem(
                detail: "n must be between 1 and 200.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid parameter");
        }

        var stories = await handler.GetBestStoriesAsync(n, cancellationToken);
        
        return Results.Ok(stories);
    } 
}