using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class DroneConfiguration : IEntityTypeConfiguration<Drone>
{
    public void Configure(EntityTypeBuilder<Drone> builder)
    {
        builder.ToTable("Drones");

        builder.Property(d => d.Name)
            .IsRequired()
            .UseCollation("NOCASE");

        builder.Property(d => d.Description)
            .IsRequired();

        builder.HasIndex(d => d.Name)
            .IsUnique();
    }
}
