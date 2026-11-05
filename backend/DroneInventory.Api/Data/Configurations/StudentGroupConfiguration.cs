using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.ToTable("StudentGroups");

        builder.Property(g => g.Name)
            .IsRequired()
            .UseCollation("NOCASE");

        builder.HasIndex(g => g.Name)
            .IsUnique();
    }
}
