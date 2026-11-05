using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

// Quantities are flattened so the frontend tables need no logic (design D5).
public record LoanResponse(
    int Id,
    int DroneId,
    string DroneName,
    int StudentGroupId,
    string StudentGroupName,
    DateTime LoanDate,
    DateTime? ReturnDate,
    LoanStatus Status,
    int Batteries,
    int Propellers);
