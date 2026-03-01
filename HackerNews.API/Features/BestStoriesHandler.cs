using Microsoft.Extensions.Caching.Memory;

namespace HackerNews.Features;

public class BestStoriesHandler
{
    private const int CacheTtlSeconds = 300;
    
    private readonly HackerNewsClient _hackerNewsClient;
    private readonly IMemoryCache _cache;

    public BestStoriesHandler(HackerNewsClient hackerNewsClient, IMemoryCache cache)
    {
        _hackerNewsClient = hackerNewsClient;
        _cache = cache;
    }
    
    public async Task<IEnumerable<GetBestStoriesResponse>> GetBestStoriesAsync(int n, CancellationToken cancellationToken = default)
    {
        var ids = await _cache.GetOrCreateAsync("hackernews:beststories:ids", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheTtlSeconds);
            return await _hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);
        }) ?? [];
        
        var tasks = ids.Take(n).Select(id => _cache.GetOrCreateAsync($"hackernews:story:{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheTtlSeconds);
            return await _hackerNewsClient.GetStoryAsync(id, cancellationToken);
        }));
        
        var stories = await Task.WhenAll(tasks);

        return stories
            .Where(s => s is not null)
            .Select(s => new GetBestStoriesResponse(s!.Title, s.Url, s.Score, s.By, DateTimeOffset.FromUnixTimeSeconds(s.Time), s.Descendants))
            .OrderByDescending(s => s.Score);
    }
}