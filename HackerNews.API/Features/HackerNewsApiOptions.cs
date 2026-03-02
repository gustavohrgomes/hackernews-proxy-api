using System.ComponentModel.DataAnnotations;

namespace HackerNews.Features;

public class HackerNewsApiOptions
{
    public const string SectionName = "HackerNewsApi";

    [Required, Url]
    public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";
}

public class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(1, int.MaxValue)]
    public int OutputCacheTtlSeconds { get; set; } = 15;

    [Range(1, int.MaxValue)]
    public int MemoryCacheTtlSeconds { get; set; } = 300;
}
