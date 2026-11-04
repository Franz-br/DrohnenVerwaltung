namespace DroneInventory.Api.Domain;

public class NotFoundException(string message) : Exception(message);
