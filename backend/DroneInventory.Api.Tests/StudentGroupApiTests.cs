using System.Net;
using System.Net.Http.Json;
using DroneInventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneInventory.Api.Tests;

public class StudentGroupApiTests
{
    [Fact]
    public async Task Create_a_student_group_stores_it_with_an_identifier_and_its_name()
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<StudentGroupDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Group 3", created.Name);

        var groups = await client.GetFromJsonAsync<List<StudentGroupDto>>("/api/student-groups");
        var group = Assert.Single(groups!);
        Assert.Equal(created, group);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_a_student_group_without_a_name_is_rejected_and_stores_nothing(string name)
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/student-groups", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var groups = await client.GetFromJsonAsync<List<StudentGroupDto>>("/api/student-groups");
        Assert.Empty(groups!);
    }

    [Theory]
    [InlineData("Group 3")]
    [InlineData("group 3")]
    public async Task Create_a_student_group_with_a_duplicate_name_is_rejected(string duplicateName)
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);
        (await client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" })).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/student-groups", new { name = duplicateName });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var groups = await client.GetFromJsonAsync<List<StudentGroupDto>>("/api/student-groups");
        Assert.Single(groups!);
    }

    [Fact]
    public async Task List_returns_all_student_groups_with_their_name()
    {
        using var factory = new DroneInventoryApiFactory();
        var client = await CreateClientAsync(factory);
        (await client.PostAsJsonAsync("/api/student-groups", new { name = "Group 3" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/student-groups", new { name = "Group 1" })).EnsureSuccessStatusCode();

        var groups = await client.GetFromJsonAsync<List<StudentGroupDto>>("/api/student-groups");

        Assert.NotNull(groups);
        Assert.Equal(["Group 1", "Group 3"], groups.Select(group => group.Name));
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

    private sealed record StudentGroupDto(int Id, string Name);
}
