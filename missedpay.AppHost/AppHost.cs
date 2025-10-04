var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.missedpay_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
