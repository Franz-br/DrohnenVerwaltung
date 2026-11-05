using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Contracts;

// The loan date and status are set by the backend, so they are never part of the request.
public record CreateLoanRequest(int DroneId, int StudentGroupId, IReadOnlyList<LoanItemRequest>? Items);

public record LoanItemRequest(LoanItemType Type, int Quantity);
