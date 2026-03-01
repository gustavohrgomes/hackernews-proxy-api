namespace HackerNews.Features;

public record GetBestStoriesResponse(
    string Title,
    string? Uri,
    int Score,
    string PostedBy,
    DateTimeOffset Time,
    int CommentCount);