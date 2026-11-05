using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Application;

public class LoanService(DroneInventoryDbContext db, TimeProvider timeProvider)
{
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
