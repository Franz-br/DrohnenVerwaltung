namespace DroneInventory.Api.Domain;

// Derived from the drone's active loan; never stored in the database.
public enum DroneStatus
{
    Available,
    Loaned
}
