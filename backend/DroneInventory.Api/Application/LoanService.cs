using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Application;

public class LoanService(DroneInventoryDbContext db, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<LoanResponse>> ListAsync(LoanStatus status, CancellationToken cancellationToken)
    {
        return await (
                from loan in db.Loans
                join drone in db.Drones on loan.DroneId equals drone.Id
                join studentGroup in db.StudentGroups on loan.StudentGroupId equals studentGroup.Id
                where loan.Status == status
                orderby loan.LoanDate, drone.Name
                select new LoanResponse(
                    loan.Id,
                    drone.Id,
                    drone.Name,
                    studentGroup.Id,
                    studentGroup.Name,
                    loan.LoanDate,
                    loan.ReturnDate,
                    loan.Status,
                    loan.Items.Where(item => item.Type == LoanItemType.Battery).Sum(item => item.Quantity),
                    loan.Items.Where(item => item.Type == LoanItemType.Propeller).Sum(item => item.Quantity)))
            .ToListAsync(cancellationToken);
    }

    public async Task<LoanResponse> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        var drone = await db.Drones.FindAsync([request.DroneId], cancellationToken)
            ?? throw new NotFoundException($"No drone with id {request.DroneId} exists.");

        var group = await db.StudentGroups.FindAsync([request.StudentGroupId], cancellationToken)
            ?? throw new NotFoundException($"No student group with id {request.StudentGroupId} exists.");

        if (await db.Loans.AnyAsync(l => l.DroneId == drone.Id && l.Status == LoanStatus.Active, cancellationToken))
        {
            throw new ConflictException($"The drone \"{drone.Name}\" already has an active loan.");
        }

        var items = (request.Items ?? []).Select(item => (item.Type, item.Quantity));
        var loan = Loan.Create(drone.Id, group.Id, items, timeProvider.GetUtcNow().UtcDateTime);

        db.Loans.Add(loan);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The filtered unique index on active loans is the last line of defence, e.g. for
            // two simultaneous requests for the same drone (design D3).
            throw new ConflictException($"The drone \"{drone.Name}\" already has an active loan.");
        }

        return ToResponse(loan, drone, group);
    }

    public async Task<LoanResponse> ReturnAsync(int id, CancellationToken cancellationToken)
    {
        var loan = await db.Loans.Include(l => l.Items).SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No loan with id {id} exists.");

        loan.Return(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);

        var drone = await db.Drones.SingleAsync(d => d.Id == loan.DroneId, cancellationToken);
        var group = await db.StudentGroups.SingleAsync(g => g.Id == loan.StudentGroupId, cancellationToken);

        return ToResponse(loan, drone, group);
    }

    private static LoanResponse ToResponse(Loan loan, Drone drone, StudentGroup group) =>
        new(
            loan.Id,
            drone.Id,
            drone.Name,
            group.Id,
            group.Name,
            loan.LoanDate,
            loan.ReturnDate,
            loan.Status,
            loan.QuantityOf(LoanItemType.Battery),
            loan.QuantityOf(LoanItemType.Propeller));
}
