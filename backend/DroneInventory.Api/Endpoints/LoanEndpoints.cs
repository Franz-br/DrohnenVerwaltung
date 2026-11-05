using DroneInventory.Api.Application;
using DroneInventory.Api.Contracts;
using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Endpoints;

public static class LoanEndpoints
{
    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var loans = app.MapGroup("/api/loans");

        // GET /api/loans?status=Active lists the active loans (REQ-007).
        loans.MapGet("", async (LoanStatus status, LoanService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(status, cancellationToken)));

        loans.MapPost("", async (
            CreateLoanRequest request,
            LoanService service,
            CancellationToken cancellationToken) =>
        {
            var loan = await service.CreateAsync(request, cancellationToken);

            return Results.Created($"/api/loans/{loan.Id}", loan);
        });

        // The return date is set by the backend, so the request has no body (REQ-009).
        loans.MapPost("/{id:int}/return", async (int id, LoanService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ReturnAsync(id, cancellationToken)));

        return app;
    }
}
