using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Data;

public class DroneInventoryDbContext(DbContextOptions<DroneInventoryDbContext> options) : DbContext(options)
{
}
