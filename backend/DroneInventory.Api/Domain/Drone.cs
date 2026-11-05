namespace DroneInventory.Api.Domain;

public class Drone
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string Description { get; set; } = "";
}
