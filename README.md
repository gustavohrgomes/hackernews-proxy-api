# Hacker News Best Stories API

A RESTful API built with ASP.NET Core (.NET 10) that retrieves the top _n_ "best stories" from the [Hacker News API](https://github.com/HackerNews/API), sorted by score in descending order.

---

## Running the Application

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) [Docker Desktop](https://www.docker.com/products/docker-desktop/) for containerised execution
- (Optional) [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire/) if running through the AppHost orchestrator

### Run locally

```bash
cd HackerNews.API
dotnet restore
dotnet run
```

The API starts on `https://localhost:5001` (or the port shown in console output).
Swagger UI is available at `/swagger` in Development mode.

### Run with Docker Compose

```bash
docker-compose up --build
```

The API is exposed on port **8080** (HTTP) and **8081** (HTTPS).

### Run with .NET Aspire

```bash
cd HackerNews.AppHost
dotnet run
```

The Aspire dashboard opens automatically and shows the API project with health checks, traces, and metrics.

### Example request

```
GET /api/best-stories?n=3
```

Response (`200 OK`):

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "score": 1716,
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "commentCount": 572
  },
  ...
]
```

The `n` query parameter must be between **1** and **200**. Values outside this range return a `400 Bad Request` with a ProblemDetails body.

### Configuration

The API reads settings from `appsettings.json` (and environment variables). Default values match the original hardcoded behaviour:

| Key | Default | Description |
|-----|---------|-------------|
| `HackerNewsApi:BaseUrl` | `https://hacker-news.firebaseio.com/v0/` | Hacker News API base URL |
| `Cache:OutputCacheTtlSeconds` | `15` | HTTP output cache TTL (seconds) |
| `Cache:MemoryCacheTtlSeconds` | `300` | In-memory cache TTL (seconds) |

Override via environment variables using the `__` separator:

```bash
export HackerNewsApi__BaseUrl=https://custom-hn-proxy.example.com/v0/
export Cache__MemoryCacheTtlSeconds=600
```

Or in Docker Compose / Kubernetes, set them as container environment variables.

### Run tests

```bash
dotnet test
```

All tests use fake HTTP handlers — no network access to the Hacker News API is required.

---

## Architecture Overview

The solution follows a **vertical-slice** layout: all code for the "Best Stories" feature (endpoint, handler, HTTP client, DTOs) lives together under `HackerNews.API/Features/`.

```
HackerNews.API/              # Web API (Minimal APIs)
├── Features/
│   ├── BestStoriesEndpoint   # Route definition + input validation
│   ├── BestStoriesHandler    # Orchestrates cache lookups and API calls
│   ├── HackerNewsClient      # Typed wrapper over IHttpClientFactory
│   ├── StoryItem             # Upstream API model
│   └── GetBestStoriesResponse# Response DTO
├── ExceptionHandlers/
│   └── HackerNewsExceptionHandler
HackerNews.AppHost/           # .NET Aspire orchestration host
HackerNews.ServiceDefaults/   # Shared cross-cutting concerns (OpenTelemetry, resilience, health checks)
HackerNews.Tests/             # Unit and integration tests (xUnit)
```

**Request flow:**

1. `GET /api/best-stories?n=10` --> Output Cache check (15 s TTL).
2. On cache miss --> `BestStoriesHandler` fetches best-story IDs from in-memory cache (5 min TTL) or the Hacker News API.
3. The first _n_ story details are fetched **concurrently** (`Task.WhenAll`), each individually cached in memory for 5 minutes.
4. Results are projected to the response DTO and sorted by score descending.

---

## Design Decisions

### Two-layer caching strategy

| Layer | Mechanism | TTL | Purpose |
|---|---|---|---|
| **Output Cache** | ASP.NET Core `OutputCache` middleware | 15 s | Serves identical HTTP responses instantly for repeated requests with the same `n` |
| **Memory Cache** | `IMemoryCache` | 5 min | Prevents redundant calls to the Hacker News API across different values of `n` |

The short output-cache window keeps responses reasonably fresh, while the longer memory cache absorbs the vast majority of upstream traffic. Because individual stories are cached by ID, a request for `n=5` warms the cache for a subsequent `n=10`: only the 5 new stories require upstream calls.

### Avoiding overload on the Hacker News API

- **In-memory caching**: the best-story ID list and each story detail are cached for 5 minutes, so repeated or overlapping requests do not generate upstream traffic.
- **Output caching**: identical requests are served from the HTTP-level cache for 15 seconds with zero handler execution.
- **Standard resilience handler** (via `Microsoft.Extensions.Http.Resilience` / Polly): automatically applies retry with exponential back-off, circuit breaker, and request timeout to every outgoing HTTP call, protecting the upstream API during transient failures.
- **Input validation**: `n` is capped at 200, bounding the maximum number of concurrent upstream calls on a cold cache.

### HTTP client usage

`HackerNewsClient` uses `IHttpClientFactory` (named client) rather than a static `HttpClient`. The factory manages DNS rotation, connection pooling, and handler lifetime automatically. The Aspire `ServiceDefaults` layer attaches resilience and service-discovery behaviours globally to all factory-created clients.

### Error handling

`HackerNewsExceptionHandler` catches `HttpRequestException`, `TaskCanceledException`, and `OperationCanceledException` and returns a structured `502 Bad Gateway` ProblemDetails response. This gives callers a clear signal that the upstream API is unreachable without leaking internal details.

### Vertical-slice organisation

All code for a feature is co-located in one folder. This keeps navigation simple and avoids the ceremony of separate layers (Controllers → Services → Repositories) for a small, focused API.

---

## Assumptions

1. The Hacker News `beststories.json` endpoint returns IDs already ranked by "best": the API re-sorts the first _n_ by score as required by the spec.
2. Stories may not have a URL (e.g. "Ask HN" posts), so `uri` is nullable in the response.
3. A maximum of 200 stories is a reasonable upper bound for a single request; callers needing more can paginate or make multiple calls.
4. In-memory caching is acceptable for a single-instance deployment; distributed caching is not needed at this stage.
5. The Hacker News API is publicly available and does not require authentication.

---

## Limitations and Trade-offs

- **No distributed cache.** Scaling to multiple instances would cause each instance to maintain its own cache, multiplying upstream traffic. A shared cache (e.g. Redis) would be needed.
- **Cache stampede risk.** `IMemoryCache.GetOrCreateAsync` does not serialise concurrent calls for the same key. Under high concurrency on a cold cache, multiple threads may hit the Hacker News API for the same story before the first response is cached.
- **Cold-cache burst.** A request for `n=200` on a cold cache fires up to 200 concurrent HTTP requests to the Hacker News API simultaneously. While the Polly resilience pipeline provides some protection, explicit concurrency throttling (e.g. `SemaphoreSlim` or `Parallel.ForEachAsync` with a `MaxDegreeOfParallelism`) would be safer.
- **Exception handler may not be active.** `AddExceptionHandler<T>()` is called but `app.UseExceptionHandler()` is not explicitly invoked in the middleware pipeline, which means the custom handler may not intercept exceptions as intended.

---

## Potential Improvements (prioritised)

1. **Throttle concurrent upstream calls** with `SemaphoreSlim` or `Parallel.ForEachAsync` to cap the number of in-flight requests to the Hacker News API. _(reliability)_
2. **Address cache stampede** by using `LazyCache`, `FusionCache`, or a custom `SemaphoreSlim`-based wrapper around `IMemoryCache` to ensure only one caller populates a given cache entry. _(reliability under load)_
3. **Switch to a distributed cache (Redis)** for multi-instance deployments, ideally wired through Aspire's Redis component. _(horizontal scalability)_
4. **Add pagination** for large result sets instead of relying solely on the `n` parameter cap. _(API usability)_
5. **Implement background cache refresh** (e.g. periodic `IHostedService`) to pre-warm the cache and eliminate cold-start latency entirely. _(performance, user experience)_
6. **Add rate limiting middleware** (`Microsoft.AspNetCore.RateLimiting`) to protect the API itself from excessive client traffic. _(production hardening)_
