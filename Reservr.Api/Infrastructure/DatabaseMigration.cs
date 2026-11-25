using Microsoft.EntityFrameworkCore;

namespace Reservr.Api.Infrastructure;

internal static class DatabaseMigration
{
    /// <summary>Applies pending EF Core migrations. Called at startup in Development only.</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservrDbContext>();
        await db.Database.MigrateAsync();
    }
}
