using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests;

public class LoanPersistenceTests : IDisposable
{
    private static readonly DateTime LoanDate = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly int _tello07Id;
    private readonly int _group3Id;

    public LoanPersistenceTests()
    {
        _connection.Open();

        using var db = CreateContext();
        db.Database.Migrate();

        var drone = new Drone { Name = "TELLO-07" };
        var group = new StudentGroup { Name = "Group 3" };
        db.AddRange(drone, group);
        db.SaveChanges();

        _tello07Id = drone.Id;
        _group3Id = group.Id;
    }

    private DroneInventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DroneInventoryDbContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task Loan_is_stored_with_its_loan_items()
    {
        await using (var db = CreateContext())
        {
            db.Loans.Add(Loan.Create(_tello07Id, _group3Id, [(LoanItemType.Battery, 4), (LoanItemType.Propeller, 8)], LoanDate));
            await db.SaveChangesAsync();
        }

        await using var readDb = CreateContext();
        var loan = await readDb.Loans.Include(l => l.Items).SingleAsync();
        Assert.Equal(_tello07Id, loan.DroneId);
        Assert.Equal(_group3Id, loan.StudentGroupId);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(LoanDate, loan.LoanDate);
        Assert.Equal(DateTimeKind.Utc, loan.LoanDate.Kind);
        Assert.Null(loan.ReturnDate);
        Assert.Equal(4, loan.QuantityOf(LoanItemType.Battery));
        Assert.Equal(8, loan.QuantityOf(LoanItemType.Propeller));
    }

    [Fact]
    public async Task Second_active_loan_for_the_same_drone_fails_at_the_database()
    {
        await using (var db = CreateContext())
        {
            db.Loans.Add(Loan.Create(_tello07Id, _group3Id, [], LoanDate));
            await db.SaveChangesAsync();
        }

        await using var secondDb = CreateContext();
        secondDb.Loans.Add(Loan.Create(_tello07Id, _group3Id, [], LoanDate));
        await Assert.ThrowsAsync<DbUpdateException>(() => secondDb.SaveChangesAsync());
    }

    [Fact]
    public async Task Returned_loans_do_not_block_a_new_active_loan_for_the_same_drone()
    {
        // Loan.Return() comes with task 4.6, so the returned loan is inserted directly.
        await using var db = CreateContext();
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO Loans (DroneId, StudentGroupId, LoanDate, ReturnDate, Status) VALUES ({_tello07Id}, {_group3Id}, {LoanDate}, {LoanDate}, 'Returned')");

        db.Loans.Add(Loan.Create(_tello07Id, _group3Id, [], LoanDate));
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Loans.CountAsync(l => l.DroneId == _tello07Id));
    }

    [Fact]
    public async Task Loan_for_an_unknown_drone_fails_at_the_database()
    {
        await using var db = CreateContext();
        db.Loans.Add(Loan.Create(droneId: 999, _group3Id, [], LoanDate));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Negative_quantity_fails_at_the_database()
    {
        await using var db = CreateContext();
        db.Loans.Add(Loan.Create(_tello07Id, _group3Id, [], LoanDate));
        await db.SaveChangesAsync();
        var loanId = (await db.Loans.SingleAsync()).Id;

        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlAsync(
            $"INSERT INTO LoanItems (LoanId, Type, Quantity) VALUES ({loanId}, 'Battery', -1)"));
    }

    public void Dispose() => _connection.Dispose();
}
