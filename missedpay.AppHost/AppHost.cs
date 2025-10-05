var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var missedpaydb = postgres.AddDatabase("missedpaydb");

var apiService = builder.AddProject<Projects.missedpay_ApiService>("apiservice")
    .WithReference(missedpaydb)
    .WaitFor(postgres)
    .WithHttpHealthCheck("/health");

builder.AddViteApp(name: "frontend", workingDirectory: "../missedpay.Frontend")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithNpmPackageInstallation();

builder.Build().Run();
