namespace HackerNews.Features;

public record StoryItem(
    int Id,
    string Title,
    string? Url,
    int Score,
    string By,
    long Time,
    int Descendants);