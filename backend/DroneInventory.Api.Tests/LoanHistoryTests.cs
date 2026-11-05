using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class LoanHistoryTests : IDisposable
{
    // The fixed time of DroneInventoryApiFactory, used as loan date and return date.
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly DroneInventoryApiFactory _factory = new();

    [Fact]
    public async Task Returned_loan_appears_in_history_unchanged_with_its_return_date()
    {
        var client = await CreateClientAsync();
        var tello07 = await PostAsync(client, "/api/drones", new { name = "TELLO-07", description = "" });
        var group3 = await PostAsync(client, "/api/student-groups", new { name = "Group 3" });
        var loanId = await LoanAsync(client, tello07, group3, batteries: 4, propellers: 8);

        (await client.PostAsync($"/api/loans/{loanId}/return", null)).EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<List<LoanDto>>("/api/loans?status=Returned");
        var loan = Assert.Single(history!);
        Assert.Equal(loanId, loan.Id);
        Assert.Equal("Group 3", loan.StudentGroupName);
        Assert.Equal("TELLO-07", loan.DroneName);
        Assert.Equal(4, loan.Batteries);
        Assert.Equal(8, loan.Propellers);
        Assert.Equal(Now, loan.LoanDate.ToUniversalTime());
        Assert.Equal(Now, loan.ReturnDate!.Value.ToUniversalTime());
        Assert.Equal("Returned", loan.Status);

        var active = await client.GetFromJsonAsync<List<LoanDto>>("/api/loans?status=Active");
        Assert.Empty(active!);
    }

    [Fact]
    public async Task History_keeps_several_loans_of_the_same_drone()
    {
        var client = await CreateClientAsync();
        var tello07 = await PostAsync(client, "/api/drones", new { name = "TELLO-07", description = "" });
        var group3 = await PostAsync(client, "/api/student-groups", new { name = "Group 3" });
        var group1 = await PostAsync(client, "/api/student-groups", new { name = "Group 1" });

        var firstLoan = await LoanAsync(client, tello07, group3, batteries: 4, propellers: 8);
        (await client.PostAsync($"/api/loans/{firstLoan}/return", null)).EnsureSuccessStatusCode();
        var secondLoan = await LoanAsync(client, tello07, group1, batteries: 2, propellers: 4);
        (await client.PostAsync($"/api/loans/{secondLoan}/return", null)).EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<List<LoanDto>>("/api/loans?status=Returned");

        Assert.NotNull(history);
        Assert.Equal([firstLoan, secondLoan], history.Select(l => l.Id).Order());
        Assert.All(history, l => Assert.Equal("TELLO-07", l.DroneName));
    }

    [Fact]
    public async Task Loans_cannot_be_deleted()
    {
        var client = await CreateClientAsync();
        var tello07 = await PostAsync(client, "/api/drones", new { name = "TELLO-07", description = "" });
        var group3 = await PostAsync(client, "/api/student-groups", new { name = "Group 3" });
        var loanId = await LoanAsync(client, tello07, group3, batteries: 0, propellers: 0);
        (await client.PostAsync($"/api/loans/{loanId}/return", null)).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/loans/{loanId}");

        Assert.False(response.IsSuccessStatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        Assert.Equal(1, await db.Loans.CountAsync());
    }

    private static async Task<int> LoanAsync(HttpClient client, int droneId, int studentGroupId, int batteries, int propellers) =>
        await PostAsync(client, "/api/loans", new
        {
            droneId,
            studentGroupId,
            items = new[] { new { type = "Battery", quantity = batteries }, new { type = "Propeller", quantity = propellers } }
        });

    private static async Task<int> PostAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
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

    public void Dispose() => _factory.Dispose();

    private sealed record IdDto(int Id);

    private sealed record LoanDto(
        int Id,
        string DroneName,
        string StudentGroupName,
        DateTime LoanDate,
        DateTime? ReturnDate,
        string Status,
        int Batteries,
        int Propellers);
}
