using DroneInventory.Api.Application;
using DroneInventory.Api.Contracts;

namespace DroneInventory.Api.Endpoints;

public static class LoanEndpoints
{
    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var loans = app.MapGroup("/api/loans");

        loans.MapPost("", async (
            CreateLoanRequest request,
            LoanService service,
            CancellationToken cancellationToken) =>
        {
            var loan = await service.CreateAsync(request, cancellationToken);

            return Results.Created($"/api/loans/{loan.Id}", loan);
        });

        return app;
    }
}
