using Microsoft.EntityFrameworkCore;
using Reservr.Domain;

namespace Reservr.Api.Infrastructure;

public sealed class ReservrDbContext(DbContextOptions<ReservrDbContext> options) : DbContext(options)
{
    public DbSet<Lane> Lanes => Set<Lane>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each entity has its own IEntityTypeConfiguration in this folder,
        // so adding an entity does not mean editing this file.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservrDbContext).Assembly);
    }
}
