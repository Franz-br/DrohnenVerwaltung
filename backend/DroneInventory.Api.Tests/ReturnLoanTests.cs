using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class ReturnLoanTests : IDisposable
{
    // The fixed time of DroneInventoryApiFactory.
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly DroneInventoryApiFactory _factory = new();

    [Fact]
    public async Task Return_a_loan_marks_it_as_returned_and_makes_the_drone_available_again()
    {
        var client = await CreateClientAsync();
        var loanId = await LoanTello07ToGroup3Async(client);

        var response = await client.PostAsync($"/api/loans/{loanId}/return", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        Assert.Equal("Returned", loan!.Status);

        var drones = await client.GetFromJsonAsync<List<DroneDto>>("/api/drones");
        Assert.Equal("Available", Assert.Single(drones!).Status);
    }

    [Fact]
    public async Task Return_stores_the_time_of_the_return_as_return_date()
    {
        var client = await CreateClientAsync();
        var loanId = await LoanTello07ToGroup3Async(client);

        var response = await client.PostAsync($"/api/loans/{loanId}/return", null);

        var loan = await response.Content.ReadFromJsonAsync<LoanDto>();
        Assert.Equal(Now, loan!.ReturnDate!.Value.ToUniversalTime());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>();
        Assert.Equal(Now, (await db.Loans.SingleAsync()).ReturnDate);
    }

    [Fact]
    public async Task Return_of_a_returned_loan_is_rejected_as_a_conflict()
    {
        var client = await CreateClientAsync();
        var loanId = await LoanTello07ToGroup3Async(client);
        (await client.PostAsync($"/api/loans/{loanId}/return", null)).EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/loans/{loanId}/return", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Return_of_an_unknown_loan_is_rejected_as_not_found()
    {
        var client = await CreateClientAsync();

        var response = await client.PostAsync("/api/loans/999/return", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<int> LoanTello07ToGroup3Async(HttpClient client)
    {
        var drone = await PostAsync(client, "/api/drones", new { name = "TELLO-07", description = "" });
        var group = await PostAsync(client, "/api/student-groups", new { name = "Group 3" });
        return await PostAsync(client, "/api/loans", new { droneId = drone, studentGroupId = group });
    }

    private static async Task<int> PostAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
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

    private sealed record DroneDto(int Id, string Name, string Status);

    private sealed record LoanDto(int Id, DateTime? ReturnDate, string Status);
}
