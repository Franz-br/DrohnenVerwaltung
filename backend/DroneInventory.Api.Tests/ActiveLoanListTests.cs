using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class ActiveLoanListTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly DroneInventoryApiFactory _factory = new();

    [Fact]
    public async Task Active_loans_are_listed_with_group_drone_batteries_propellers_and_loan_date()
    {
        var client = await CreateClientAsync();
        var tello02 = await CreateAsync(client, "/api/drones", new { name = "TELLO-02", description = "" });
        var tello07 = await CreateAsync(client, "/api/drones", new { name = "TELLO-07", description = "" });
        var group1 = await CreateAsync(client, "/api/student-groups", new { name = "Group 1" });
        var group3 = await CreateAsync(client, "/api/student-groups", new { name = "Group 3" });
        await LoanAsync(client, tello02, group1, batteries: 3, propellers: 4);
        await LoanAsync(client, tello07, group3, batteries: 4, propellers: 8);

        var loans = await client.GetFromJsonAsync<List<LoanDto>>("/api/loans?status=Active");

        Assert.NotNull(loans);
        Assert.Equal(2, loans.Count);
        Assert.Equal(new LoanDto("Group 1", "TELLO-02", 3, 4, Now, "Active"), Normalize(loans[0]));
        Assert.Equal(new LoanDto("Group 3", "TELLO-07", 4, 8, Now, "Active"), Normalize(loans[1]));
    }

    [Fact]
    public async Task Returned_loan_is_not_listed_as_active()
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

        var loans = await client.GetFromJsonAsync<List<LoanDto>>("/api/loans?status=Active");

        Assert.Empty(loans!);
    }

    [Fact]
    public async Task Unknown_status_is_rejected()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/api/loans?status=Lost");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static LoanDto Normalize(LoanDto loan) => loan with { LoanDate = loan.LoanDate.ToUniversalTime() };

    private static async Task LoanAsync(HttpClient client, int droneId, int studentGroupId, int batteries, int propellers)
    {
        var response = await client.PostAsJsonAsync("/api/loans", new
        {
            droneId,
            studentGroupId,
            items = new[] { new { type = "Battery", quantity = batteries }, new { type = "Propeller", quantity = propellers } }
        });
        response.EnsureSuccessStatusCode();
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
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    public void Dispose() => _factory.Dispose();

    private sealed record IdDto(int Id);

    // Only the columns of the active loan list (REQ-007) plus the status.
    private sealed record LoanDto(
        string StudentGroupName,
        string DroneName,
        int Batteries,
        int Propellers,
        DateTime LoanDate,
        string Status);
}
