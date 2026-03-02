using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HackerNews.Features;
using HackerNews.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HackerNews.Tests.Features;

public class BestStoriesEndpointTests : IClassFixture<BestStoriesEndpointTests.Factory>
{
    private readonly HttpClient _client;

    public BestStoriesEndpointTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBestStories_Returns200_WithValidN()
    {
        var response = await _client.GetAsync("/api/best-stories?n=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stories = await response.Content.ReadFromJsonAsync<GetBestStoriesResponse[]>();
        stories.Should().NotBeNull();
        stories!.Length.Should().Be(2);
    }

    [Fact]
    public async Task GetBestStories_ReturnsSortedByScoreDescending()
    {
        var response = await _client.GetAsync("/api/best-stories?n=3");
        var stories = await response.Content.ReadFromJsonAsync<GetBestStoriesResponse[]>();

        stories.Should().NotBeNull();
        stories!.Select(s => s.Score).Should().BeInDescendingOrder();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    public async Task GetBestStories_Returns400_ForInvalidN(int n)
    {
        var response = await _client.GetAsync($"/api/best-stories?n={n}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
    }

    [Fact]
    public async Task GetBestStories_Returns400_WhenNIsMissing()
    {
        var response = await _client.GetAsync("/api/best-stories");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBestStories_ResponseShape_MatchesSpec()
    {
        var response = await _client.GetAsync("/api/best-stories?n=1");
        var stories = await response.Content.ReadFromJsonAsync<GetBestStoriesResponse[]>();

        stories.Should().NotBeNull();
        var story = stories!.First();

        story.Title.Should().NotBeNullOrEmpty();
        story.PostedBy.Should().NotBeNullOrEmpty();
        story.Score.Should().BePositive();
        story.CommentCount.Should().BeGreaterThanOrEqualTo(0);
    }

    public class Factory : WebApplicationFactory<Program>
    {
        private readonly FakeHttpHandler _handler = new();
        private static readonly int[] body = [1, 2, 3];

        public Factory()
        {
            var stories = StoryFixtures.CreateBatch((1, 300), (2, 100), (3, 500));

            _handler.SetupJson("beststories.json", body);
            foreach (var story in stories)
                _handler.SetupJson($"item/{story.Id}.json", story);
        }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient("HackerNewsApi", client =>
                {
                    client.BaseAddress = new Uri("https://fake-hn.example.com/v0/");
                }).ConfigurePrimaryHttpMessageHandler(() => _handler);
            });
        }
    }
}
