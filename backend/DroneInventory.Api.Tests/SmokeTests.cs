using System.Net;
using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class SmokeTests(DroneInventoryApiFactory factory) : IClassFixture<DroneInventoryApiFactory>
{
    [Fact]
    public async Task Api_starts_with_in_memory_sqlite_and_fixed_time()
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        Assert.True(db.Database.IsSqlite());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync());

        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero), timeProvider.GetUtcNow());

        var response = await factory.CreateClient().GetAsync("/api/unknown");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
