using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservr.Domain;

namespace Reservr.Api.Infrastructure;

internal sealed class LaneConfiguration : IEntityTypeConfiguration<Lane>
{
    public void Configure(EntityTypeBuilder<Lane> builder)
    {
        builder.ToTable("lanes", t => t.HasCheckConstraint("ck_lanes_number_positive", "number > 0"));
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(l => l.Number).HasColumnName("number");
    }
}
