using System.Text.Json;

namespace HackerNews.Features;

public class HackerNewsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    private readonly IHttpClientFactory _httpClientFactory;
    
    public HackerNewsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    private HttpClient Client => _httpClientFactory.CreateClient("HackerNewsApi");

    public async Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await Client.GetFromJsonAsync<int[]>("beststories.json", JsonOptions, cancellationToken);
        return ids ?? [];
    }
    
    public async Task<StoryItem?> GetStoryAsync(int id, CancellationToken cancellationToken = default) => 
        await Client.GetFromJsonAsync<StoryItem>($"item/{id}.json", JsonOptions, cancellationToken);
}