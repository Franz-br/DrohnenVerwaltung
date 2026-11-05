using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class DroneApiTests
{
    [Fact]
    public async Task Create_a_drone_stores_its_name_and_description_with_status_available()
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/drones", new
        {
            name = "TELLO-07",
            description = "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<DroneDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("TELLO-07", created.Name);
        Assert.Equal("SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt", created.Description);
        Assert.Equal("Available", created.Status);

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");
        var drone = Assert.Single(drones!);
        Assert.Equal(created.Id, drone.Id);
        Assert.Equal("TELLO-07", drone.Name);
    }

    [Fact]
    public async Task Create_a_drone_without_a_name_is_rejected_and_stores_nothing()
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/drones", new
        {
            name = "   ",
            description = "SSID: TELLO-9FA21B"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");
        Assert.Empty(drones!);
    }

    [Fact]
    public async Task Create_a_drone_with_a_duplicate_name_is_rejected()
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);
        await CreateTELLO07Async(client);

        var response = await client.PostAsJsonAsync("/api/drones", new
        {
            name = "TELLO-07",
            description = "second drone"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");
        Assert.Single(drones!);
    }

    [Fact]
    public async Task List_returns_all_drones_with_name_status_and_description()
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);
        await CreateTELLO07Async(client);
        await client.PostAsJsonAsync("/api/drones", new { name = "TELLO-02", description = "reserve" });

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");

        Assert.NotNull(drones);
        Assert.Equal(2, drones.Count);

        var tello07 = drones.Single(drone => drone.Name == "TELLO-07");
        Assert.Equal("Available", tello07.Status);
        Assert.Equal("SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt", tello07.Description);

        var tello02 = drones.Single(drone => drone.Name == "TELLO-02");
        Assert.Equal("Available", tello02.Status);
        Assert.Equal("reserve", tello02.Description);
    }

    [Fact]
    public async Task List_returns_an_empty_list_when_no_drones_exist()
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");

        Assert.Empty(drones!);
    }

    private static async Task CreateTELLO07Async(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/drones", new
        {
            name = "TELLO-07",
            description = "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt"
        });

        response.EnsureSuccessStatusCode();
    }

    // The testing environment does not run migrations at startup, so the schema for the
    // in-memory database is created here once per test.
    private static async Task<HttpClient> CreateClientAsync(DroneInventoryApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        await db.Database.MigrateAsync();

        return factory.CreateClient();
    }

    private sealed record DroneDto(int Id, string Name, string Description, string Status);
}
