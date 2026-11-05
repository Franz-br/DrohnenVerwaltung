using DroneInventory.Api.Contracts;
using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Application;

public class StudentGroupService(DroneInventoryDbContext db)
{
    public async Task<IReadOnlyList<StudentGroupResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.StudentGroups
            .OrderBy(group => group.Name)
            .Select(group => new StudentGroupResponse(group.Id, group.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentGroupResponse> CreateAsync(CreateStudentGroupRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            throw new ValidationException("The student group name must not be empty.");
        }

        // The name column uses the NOCASE collation, so this comparison ignores case.
        if (await db.StudentGroups.AnyAsync(group => group.Name == name, cancellationToken))
        {
            throw new ConflictException($"A student group named \"{name}\" already exists.");
        }

        var group = new StudentGroup { Name = name };

        db.StudentGroups.Add(group);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index on the name is the last line of defence, e.g. for two
            // simultaneous requests with the same name.
            throw new ConflictException($"A student group named \"{name}\" already exists.");
        }

        return new StudentGroupResponse(group.Id, group.Name);
    }
}
