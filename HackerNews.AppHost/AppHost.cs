var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.HackerNews_API>("hackernews-api");

builder.Build().Run();
