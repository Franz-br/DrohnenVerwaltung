using DroneInventory.Api.Application;
using DroneInventory.Api.Contracts;

namespace DroneInventory.Api.Endpoints;

public static class StudentGroupEndpoints
{
    public static IEndpointRouteBuilder MapStudentGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/api/student-groups");

        groups.MapGet("", async (StudentGroupService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(cancellationToken)));

        groups.MapPost("", async (
            CreateStudentGroupRequest request,
            StudentGroupService service,
            CancellationToken cancellationToken) =>
        {
            var group = await service.CreateAsync(request, cancellationToken);

            return Results.Created($"/api/student-groups/{group.Id}", group);
        });

        return app;
    }
}
