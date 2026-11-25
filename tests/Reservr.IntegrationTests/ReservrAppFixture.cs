using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace Reservr.IntegrationTests;

/// <summary>
/// Starts the whole AppHost (PostgreSQL container + API) once for all tests in
/// the collection. Requires Docker.
/// </summary>
public sealed class ReservrAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    public DistributedApplication App { get; private set; } = null!;

    public HttpClient Api { get; private set; } = null!;

    public string DatabaseConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(StartupTimeout);

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Reservr_AppHost>(cts.Token);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.", LogLevel.Warning);
        });
        appHost.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        App = await appHost.BuildAsync(cts.Token);
        await App.StartAsync(cts.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("api", cts.Token);

        Api = App.CreateHttpClient("api");
        DatabaseConnectionString = await App.GetConnectionStringAsync("reservr", cts.Token)
            ?? throw new InvalidOperationException("Connection string for 'reservr' was not found.");
    }

    public async Task DisposeAsync()
    {
        Api?.Dispose();
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ReservrAppCollection : ICollectionFixture<ReservrAppFixture>
{
    public const string Name = "Reservr app";
}
