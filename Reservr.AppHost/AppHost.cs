var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("reservr");

// The "http" launch profile avoids depending on a trusted HTTPS dev certificate.
builder.AddDotnetProject("api", "../Reservr.Api/Reservr.Api.csproj", options => options.LaunchProfileName = "http")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
