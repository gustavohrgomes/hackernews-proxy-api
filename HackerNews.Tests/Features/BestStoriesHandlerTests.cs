using FluentAssertions;
using HackerNews.Features;
using HackerNews.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HackerNews.Tests.Features;

public class BestStoriesHandlerTests : IDisposable
{
    private readonly FakeHttpHandler _httpHandler = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly BestStoriesHandler _sut;

    public BestStoriesHandlerTests()
    {
        var client = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("https://fake-hn.example.com/v0/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("HackerNewsApi").Returns(client);

        var hackerNewsClient = new HackerNewsClient(factory);
        var cacheOptions = Options.Create(new CacheOptions());
        _sut = new BestStoriesHandler(hackerNewsClient, _cache, cacheOptions);
    }

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsExactlyN_WhenEnoughStoriesExist()
    {
        SetupIds(1, 2, 3, 4, 5);
        SetupStory(StoryFixtures.Create(1, 100));
        SetupStory(StoryFixtures.Create(2, 200));
        SetupStory(StoryFixtures.Create(3, 300));
        SetupStory(StoryFixtures.Create(4, 400));
        SetupStory(StoryFixtures.Create(5, 500));

        var result = (await _sut.GetBestStoriesAsync(3)).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsFewerThanN_WhenUpstreamHasFewerStories()
    {
        SetupIds(1, 2);
        SetupStory(StoryFixtures.Create(1, 100));
        SetupStory(StoryFixtures.Create(2, 200));

        var result = (await _sut.GetBestStoriesAsync(5)).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBestStoriesAsync_SortsByScoreDescending()
    {
        SetupIds(1, 2, 3);
        SetupStory(StoryFixtures.Create(1, 50));
        SetupStory(StoryFixtures.Create(2, 300));
        SetupStory(StoryFixtures.Create(3, 150));

        var result = (await _sut.GetBestStoriesAsync(3)).ToList();

        result.Select(s => s.Score).Should().BeInDescendingOrder();
        result[0].Score.Should().Be(300);
        result[1].Score.Should().Be(150);
        result[2].Score.Should().Be(50);
    }

    [Fact]
    public async Task GetBestStoriesAsync_MapsFieldsCorrectly()
    {
        var story = new StoryItem(42, "Test Title", "https://example.com", 999, "testuser", 1570887781, 42);
        SetupIds(42);
        SetupStory(story);

        var result = (await _sut.GetBestStoriesAsync(1)).Single();

        result.Title.Should().Be("Test Title");
        result.Uri.Should().Be("https://example.com");
        result.Score.Should().Be(999);
        result.PostedBy.Should().Be("testuser");
        result.Time.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1570887781));
        result.CommentCount.Should().Be(42);
    }

    [Fact]
    public async Task GetBestStoriesAsync_HandlesNullUrl()
    {
        var story = new StoryItem(1, "Ask HN", null, 100, "user", 1570887781, 5);
        SetupIds(1);
        SetupStory(story);

        var result = (await _sut.GetBestStoriesAsync(1)).Single();

        result.Uri.Should().BeNull();
    }

    [Fact]
    public async Task GetBestStoriesAsync_Throws_WhenStoryFetchFails()
    {
        SetupIds(1, 2);
        SetupStory(StoryFixtures.Create(1, 100));
        // story 2 is not set up — returns 404, causing HttpRequestException

        var act = () => _sut.GetBestStoriesAsync(2);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsEmpty_WhenNoIds()
    {
        SetupIds();

        var result = (await _sut.GetBestStoriesAsync(5)).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesCachedIds_OnSecondCall()
    {
        SetupIds(1);
        SetupStory(StoryFixtures.Create(1, 100));

        await _sut.GetBestStoriesAsync(1);
        var callsAfterFirst = _httpHandler.CallCount;

        await _sut.GetBestStoriesAsync(1);

        // Second call should not fetch IDs again (cached), and story is also cached
        _httpHandler.CallCount.Should().Be(callsAfterFirst);
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesCachedStories_AcrossDifferentN()
    {
        SetupIds(1, 2, 3);
        SetupStory(StoryFixtures.Create(1, 100));
        SetupStory(StoryFixtures.Create(2, 200));
        SetupStory(StoryFixtures.Create(3, 300));

        // First call fetches stories 1 and 2
        await _sut.GetBestStoriesAsync(2);
        var callsAfterFirst = _httpHandler.CallCount;

        // Second call for n=3 should only fetch story 3 (1 and 2 are cached)
        await _sut.GetBestStoriesAsync(3);
        var newCalls = _httpHandler.CallCount - callsAfterFirst;

        // Only 1 new HTTP call for story 3 (IDs are cached too)
        newCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetBestStoriesAsync_PropagatesUpstreamException()
    {
        _httpHandler.SetupException("beststories", new HttpRequestException("Connection refused"));

        var act = () => _sut.GetBestStoriesAsync(1);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private void SetupIds(params int[] ids)
    {
        _httpHandler.SetupJson("beststories.json", ids);
    }

    private void SetupStory(StoryItem story)
    {
        _httpHandler.SetupJson($"item/{story.Id}.json", story);
    }
}
