using System.Data;
using DroneInventory.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DroneInventory.Api.Tests;

public class DroneInventoryApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<DroneInventoryDbContext>>();
            services.RemoveAll<DroneInventoryDbContext>();

            services.AddDbContext<DroneInventoryDbContext>(options => options.UseSqlite(_connection));
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero)));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
