using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Data;

public class DroneInventoryDbContext(DbContextOptions<DroneInventoryDbContext> options) : DbContext(options)
{
    public DbSet<Drone> Drones => Set<Drone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DroneInventoryDbContext).Assembly);
    }
}
