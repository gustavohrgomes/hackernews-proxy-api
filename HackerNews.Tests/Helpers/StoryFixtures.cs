using Bogus;
using HackerNews.Features;

namespace HackerNews.Tests.Helpers;

public static class StoryFixtures
{
    private static readonly Faker Faker = new(locale: "en") { Random = new Randomizer(42) };

    public static StoryItem Create(int id, int score, string? title = null, long time = 0) =>
        new(
            id,
            title ?? Faker.Hacker.Phrase(),
            Faker.Internet.Url(),
            score,
            Faker.Internet.UserName(),
            time != 0 ? time : Faker.Date.BetweenOffset(
                DateTimeOffset.UnixEpoch.AddYears(40),
                DateTimeOffset.UnixEpoch.AddYears(55)).ToUnixTimeSeconds(),
            Faker.Random.Int(0, 500));

    public static StoryItem[] CreateBatch(params (int Id, int Score)[] items) =>
        [.. items.Select(i => Create(i.Id, i.Score))];
}
