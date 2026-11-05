using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class LoanItemConfiguration : IEntityTypeConfiguration<LoanItem>
{
    public void Configure(EntityTypeBuilder<LoanItem> builder)
    {
        builder.ToTable("LoanItems", table =>
            table.HasCheckConstraint("CK_LoanItems_Quantity", "\"Quantity\" >= 0"));

        builder.Property(i => i.Type)
            .HasConversion<string>();

        // Each accessory type appears at most once per loan (REQ-006).
        builder.HasIndex(i => new { i.LoanId, i.Type })
            .IsUnique();
    }
}
