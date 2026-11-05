using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests;

public class StudentGroupMigrationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public StudentGroupMigrationTests()
    {
        _connection.Open();

        using var db = CreateContext();
        db.Database.Migrate();
    }

    private DroneInventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DroneInventoryDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task Migration_stores_student_group_with_name()
    {
        await using (var db = CreateContext())
        {
            db.StudentGroups.Add(new StudentGroup { Name = "Group 3" });
            await db.SaveChangesAsync();
        }

        await using var readDb = CreateContext();
        var group = await readDb.StudentGroups.SingleAsync();
        Assert.True(group.Id > 0);
        Assert.Equal("Group 3", group.Name);
    }

    [Fact]
    public async Task Migration_rejects_duplicate_student_group_name_ignoring_case()
    {
        await using (var db = CreateContext())
        {
            db.StudentGroups.Add(new StudentGroup { Name = "Group 3" });
            await db.SaveChangesAsync();
        }

        await using var secondDb = CreateContext();
        secondDb.StudentGroups.Add(new StudentGroup { Name = "group 3" });
        await Assert.ThrowsAsync<DbUpdateException>(() => secondDb.SaveChangesAsync());
    }

    public void Dispose() => _connection.Dispose();
}
