namespace DroneInventory.Api.Contracts;

// A drone's status is derived (design D1), so it is never part of the request.
public record CreateDroneRequest(string Name, string? Description);
