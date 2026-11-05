using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneInventory.Api.Data.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        // Statuses are stored as text, so the filter of the index below can use 'Active'.
        builder.Property(l => l.Status)
            .HasConversion<string>();

        // SQLite does not keep DateTimeKind, so dates are marked as UTC again when they are read.
        builder.Property(l => l.LoanDate)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(l => l.ReturnDate)
            .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        // Loans are never deleted (REQ-010), so a drone or group with loans cannot be deleted either.
        builder.HasOne<Drone>()
            .WithMany()
            .HasForeignKey(l => l.DroneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StudentGroup>()
            .WithMany()
            .HasForeignKey(l => l.StudentGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // A drone has at most one active loan (REQ-005, design D3).
        builder.HasIndex(l => l.DroneId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");

        builder.HasMany(l => l.Items)
            .WithOne()
            .HasForeignKey(i => i.LoanId);

        builder.Navigation(l => l.Items)
            .HasField("_items");
    }
}
