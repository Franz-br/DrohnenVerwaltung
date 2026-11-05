using DroneInventory.Api.Application;
using DroneInventory.Api.Contracts;

namespace DroneInventory.Api.Endpoints;

public static class DroneEndpoints
{
    public static IEndpointRouteBuilder MapDroneEndpoints(this IEndpointRouteBuilder app)
    {
        var drones = app.MapGroup("/api/drones");

        drones.MapGet("", async (DroneService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(cancellationToken)));

        drones.MapPost("", async (
            CreateDroneRequest request,
            DroneService service,
            CancellationToken cancellationToken) =>
        {
            var drone = await service.CreateAsync(request, cancellationToken);

            return Results.Created($"/api/drones/{drone.Id}", drone);
        });

        return app;
    }
}
