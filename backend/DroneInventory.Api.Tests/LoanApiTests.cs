using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class LoanApiTests : IDisposable
{
    // The fixed time of DroneInventoryApiFactory.
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly DroneInventoryApiFactory _factory = new();

    // Loan Drone

    [Fact]
    public async Task Loan_an_available_drone_creates_an_active_loan_with_todays_loan_date()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateDroneAsync(client, "TELLO-07");
        var group3 = await CreateGroupAsync(client, "Group 3");

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = group3 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(loan);
        Assert.True(loan.Id > 0);
        Assert.Equal(tello07, loan.DroneId);
        Assert.Equal("TELLO-07", loan.DroneName);
        Assert.Equal(group3, loan.StudentGroupId);
        Assert.Equal("Group 3", loan.StudentGroupName);
        Assert.Equal(Now, loan.LoanDate.ToUniversalTime());
        Assert.Null(loan.ReturnDate);
        Assert.Equal("Active", loan.Status);
        Assert.Equal(1, await CountLoansAsync());
    }

    [Fact]
    public async Task Loan_for_an_unknown_drone_is_rejected_as_not_found()
    {
        var client = await CreateClientAsync();
        var group3 = await CreateGroupAsync(client, "Group 3");

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = 999, studentGroupId = group3 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountLoansAsync());
    }

    [Fact]
    public async Task Loan_for_an_unknown_student_group_is_rejected_as_not_found()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateDroneAsync(client, "TELLO-07");

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = 999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountLoansAsync());
    }

    // No Double Loan

    [Fact]
    public async Task Second_loan_of_a_loaned_drone_is_rejected_as_a_conflict()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateDroneAsync(client, "TELLO-07");
        var group3 = await CreateGroupAsync(client, "Group 3");
        var group1 = await CreateGroupAsync(client, "Group 1");
        (await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = group3 })).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = group1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        var activeLoan = await db.Loans.SingleAsync(l => l.DroneId == tello07 && l.Status == LoanStatus.Active);
        Assert.Equal(group3, activeLoan.StudentGroupId);
    }

    [Fact]
    public async Task Drone_can_be_loaned_again_after_its_loan_was_returned()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateDroneAsync(client, "TELLO-07");
        var group3 = await CreateGroupAsync(client, "Group 3");
        var group1 = await CreateGroupAsync(client, "Group 1");

        // The return endpoint comes with task 4.6, so the returned loan is inserted directly.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO Loans (DroneId, StudentGroupId, LoanDate, ReturnDate, Status) VALUES ({tello07}, {group3}, {Now}, {Now}, 'Returned')");
        }

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = group1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        Assert.Equal("Group 1", loan!.StudentGroupName);
        Assert.Equal("Active", loan.Status);
    }

    // Loan Accessories

    [Fact]
    public async Task Loan_with_4_batteries_and_8_propellers_records_both_quantities()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateDroneAsync(client, "TELLO-07");
        var group3 = await CreateGroupAsync(client, "Group 3");

        var response = await client.PostAsJsonAsync("/api/loans", new
        {
            droneId = tello07,
            studentGroupId = group3,
            items = new[] { new { type = "Battery", quantity = 4 }, new { type = "Propeller", quantity = 8 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        Assert.Equal(4, loan!.Batteries);
        Assert.Equal(8, loan.Propellers);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        var stored = await db.Loans.Include(l => l.Items).SingleAsync();
        Assert.Equal(2, stored.Items.Count);
    }

    [Fact]
    public async Task Loan_without_accessories_records_0_batteries_and_0_propellers()
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateDroneAsync(client, "TELLO-07");
        var group3 = await CreateGroupAsync(client, "Group 3");

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = group3 });

        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        Assert.Equal(0, loan!.Batteries);
        Assert.Equal(0, loan.Propellers);
    }

    [Fact]
    public async Task Loan_with_a_negative_quantity_is_rejected()
    {
        await AssertInvalidItemsAreRejectedAsync(new[] { new { type = "Battery", quantity = -1 } });
    }

    [Fact]
    public async Task Loan_with_an_unknown_accessory_type_is_rejected()
    {
        await AssertInvalidItemsAreRejectedAsync(new[] { new { type = "Charger", quantity = 1 } });
    }

    [Fact]
    public async Task Loan_with_a_duplicate_accessory_type_is_rejected()
    {
        await AssertInvalidItemsAreRejectedAsync(new[]
        {
            new { type = "Battery", quantity = 4 },
            new { type = "Battery", quantity = 2 }
        });
    }

    private async Task AssertInvalidItemsAreRejectedAsync(object items)
    {
        var client = await CreateClientAsync();
        var tello07 = await CreateDroneAsync(client, "TELLO-07");
        var group3 = await CreateGroupAsync(client, "Group 3");

        var response = await client.PostAsJsonAsync("/api/loans", new { droneId = tello07, studentGroupId = group3, items });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountLoansAsync());
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

    private static async Task<int> CreateDroneAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/drones", new { name, description = "" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static async Task<int> CreateGroupAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/student-groups", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private async Task<int> CountLoansAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        return await db.Loans.CountAsync();
    }

    public void Dispose() => _factory.Dispose();

    private sealed record IdDto(int Id);

    private sealed record LoanDto(
        int Id,
        int DroneId,
        string DroneName,
        int StudentGroupId,
        string StudentGroupName,
        DateTime LoanDate,
        DateTime? ReturnDate,
        string Status,
        int Batteries,
        int Propellers);
}
