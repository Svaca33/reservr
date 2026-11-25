using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reservr.Api.Infrastructure;

/// <summary>
/// Used only by `dotnet ef` (e.g. `migrations add`). At runtime the connection
/// string comes from Aspire. Creating migrations does not connect to the database.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReservrDbContext>
{
    public ReservrDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReservrDbContext>()
            .UseNpgsql("Host=localhost;Database=reservr")
            .Options;

        return new ReservrDbContext(options);
    }
}
