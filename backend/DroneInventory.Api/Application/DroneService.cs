using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Application;

public class DroneService(DroneInventoryDbContext db)
{
    public async Task<IReadOnlyList<DroneResponse>> ListAsync(CancellationToken cancellationToken)
    {
        // The status is derived from the active loan of a drone (design D1). Loans are not
        // implemented yet (task 4.4), so every drone is currently available.
        return await db.Drones
            .OrderBy(drone => drone.Name)
            .Select(drone => new DroneResponse(drone.Id, drone.Name, drone.Description, DroneStatus.Available))
            .ToListAsync(cancellationToken);
    }

    public async Task<DroneResponse> CreateAsync(CreateDroneRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            throw new ValidationException("The drone name must not be empty.");
        }

        // The name column uses the NOCASE collation, so this comparison ignores case.
        if (await db.Drones.AnyAsync(drone => drone.Name == name, cancellationToken))
        {
            throw new ConflictException($"A drone named \"{name}\" already exists.");
        }

        var drone = new Drone
        {
            Name = name,
            Description = request.Description?.Trim() ?? ""
        };

        db.Drones.Add(drone);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index on the name is the last line of defence, e.g. for two
            // simultaneous requests with the same name.
            throw new ConflictException($"A drone named \"{name}\" already exists.");
        }

        return new DroneResponse(drone.Id, drone.Name, drone.Description, DroneStatus.Available);
    }
}
