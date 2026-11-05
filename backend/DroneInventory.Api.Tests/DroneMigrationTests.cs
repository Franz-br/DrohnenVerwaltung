using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests;

public class DroneMigrationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public DroneMigrationTests()
    {
        _connection.Open();

        using var db = CreateContext();
        db.Database.Migrate();
    }

    private DroneInventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DroneInventoryDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task Migration_stores_drone_with_name_and_description()
    {
        await using (var db = CreateContext())
        {
            db.Drones.Add(new Drone { Name = "TELLO-07", Description = "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt" });
            await db.SaveChangesAsync();
        }

        await using var readDb = CreateContext();
        var drone = await readDb.Drones.SingleAsync();
        Assert.True(drone.Id > 0);
        Assert.Equal("TELLO-07", drone.Name);
        Assert.Equal("SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt", drone.Description);
    }

    [Fact]
    public async Task Migration_allows_empty_description()
    {
        await using var db = CreateContext();
        db.Drones.Add(new Drone { Name = "TELLO-02" });
        await db.SaveChangesAsync();

        Assert.Equal("", (await db.Drones.SingleAsync()).Description);
    }

    [Fact]
    public async Task Migration_rejects_duplicate_drone_name_ignoring_case()
    {
        await using (var db = CreateContext())
        {
            db.Drones.Add(new Drone { Name = "TELLO-07" });
            await db.SaveChangesAsync();
        }

        await using var secondDb = CreateContext();
        secondDb.Drones.Add(new Drone { Name = "tello-07" });
        await Assert.ThrowsAsync<DbUpdateException>(() => secondDb.SaveChangesAsync());
    }

    public void Dispose() => _connection.Dispose();
}
