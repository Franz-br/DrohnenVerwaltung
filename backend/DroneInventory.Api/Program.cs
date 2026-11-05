using DroneInventory.Api.Application;
using DroneInventory.Api.Data;
using DroneInventory.Api.Endpoints;
using DroneInventory.Api.ErrorHandling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<DroneInventoryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DroneInventory")));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<StudentGroupService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<DroneInventoryDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapStudentGroupEndpoints();

app.Run();

public partial class Program;
