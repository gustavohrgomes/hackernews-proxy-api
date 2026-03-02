using FluentAssertions;
using HackerNews.Features;
using HackerNews.Tests.Helpers;
using NSubstitute;

namespace HackerNews.Tests.Features;

public class HackerNewsClientTests
{
    private readonly FakeHttpHandler _httpHandler = new();
    private readonly HackerNewsClient _sut;

    public HackerNewsClientTests()
    {
        var client = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("https://fake-hn.example.com/v0/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("HackerNewsApi").Returns(client);

        _sut = new HackerNewsClient(factory);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ReturnsIds()
    {
        _httpHandler.SetupJson("beststories.json", new[] { 1, 2, 3 });

        var result = await _sut.GetBestStoryIdsAsync();

        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ReturnsEmpty_WhenNull()
    {
        _httpHandler.SetupJson<int[]?>("beststories.json", null);

        var result = await _sut.GetBestStoryIdsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStoryAsync_ReturnsStoryItem()
    {
        var story = StoryFixtures.Create(42, 500, "Test", 1570887781);
        _httpHandler.SetupJson("item/42.json", story);

        var result = await _sut.GetStoryAsync(42);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Score.Should().Be(500);
        result.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetStoryAsync_ThrowsHttpRequestException_WhenNotFound()
    {
        var act = () => _sut.GetStoryAsync(999);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ThrowsOnNetworkFailure()
    {
        _httpHandler.SetupException("beststories", new HttpRequestException("DNS failure"));

        var act = () => _sut.GetBestStoryIdsAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
