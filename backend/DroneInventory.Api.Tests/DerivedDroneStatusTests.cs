using System.Net.Http.Json;
using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class DerivedDroneStatusTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly DroneInventoryApiFactory _factory = new();

    [Fact]
    public async Task Drone_with_an_active_loan_is_listed_as_loaned()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateAsync(client, "/api/drones", new { name = "TELLO-07", description = "" });
        await CreateAsync(client, "/api/drones", new { name = "TELLO-02", description = "" });
        var group3 = await CreateAsync(client, "/api/student-groups", new { name = "Group 3" });

        (await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = group3 })).EnsureSuccessStatusCode();

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");
        Assert.NotNull(drones);
        Assert.Equal("Loaned", drones.Single(d => d.Name == "TELLO-07").Status);
        Assert.Equal("Available", drones.Single(d => d.Name == "TELLO-02").Status);
    }

    [Fact]
    public async Task Drone_with_only_returned_loans_is_listed_as_available()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateAsync(client, "/api/drones", new { name = "TELLO-07", description = "" });
        var group3 = await CreateAsync(client, "/api/student-groups", new { name = "Group 3" });

        // The return endpoint comes with task 4.6, so the returned loan is inserted directly.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO Loans (DroneId, StudentGroupId, LoanDate, ReturnDate, Status) VALUES ({tello07}, {group3}, {Now}, {Now}, 'Returned')");
        }

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");
        Assert.Equal("Available", Assert.Single(drones!).Status);
    }

    // The testing environment does not run migrations at startup, so the schema for the
    // in-memory database is created here once per test.
    private async Task<HttpClient> CreateClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        await db.Database.MigrateAsync();

        return _factory.CreateClient();
    }

    private static async Task<int> CreateAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DroneDto>())!.Id;
    }

    public void Dispose() => _factory.Dispose();

    private sealed record DroneDto(int Id, string Name, string? Status);
}
