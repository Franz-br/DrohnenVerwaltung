using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

public record DroneResponse(int Id, string Name, string Description, DroneStatus Status);
