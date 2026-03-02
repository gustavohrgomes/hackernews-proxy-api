using System.Net;
using System.Text.Json;

namespace HackerNews.Tests.Helpers;

/// <summary>
/// A test double for HttpMessageHandler that returns preconfigured responses
/// based on the request URI path.
/// </summary>
public sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public int CallCount { get; private set; }

    public void SetupJson<T>(string pathContains, T body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses[pathContains] = _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
        };
    }

    public void SetupFailure(string pathContains, HttpStatusCode statusCode)
    {
        _responses[pathContains] = _ => new HttpResponseMessage(statusCode);
    }

    public void SetupException(string pathContains, Exception exception)
    {
        _responses[pathContains] = _ => throw exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var path = request.RequestUri?.ToString() ?? string.Empty;

        foreach (var (key, factory) in _responses)
        {
            if (path.Contains(key, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(factory(request));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
