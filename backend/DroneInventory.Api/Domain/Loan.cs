namespace DroneInventory.Api.Domain;

// Aggregate root: a loan owns its loan items, and every rule about them is checked here.
public class Loan
{
    private readonly List<LoanItem> _items = [];

    // Used by EF Core when loading a loan from the database.
    private Loan()
    {
    }

    public int Id { get; private set; }

    public int DroneId { get; private set; }

    public int StudentGroupId { get; private set; }

    public DateTime LoanDate { get; private set; }

    public DateTime? ReturnDate { get; private set; }

    public LoanStatus Status { get; private set; }

    public IReadOnlyList<LoanItem> Items => _items;

    public static Loan Create(
        int droneId,
        int studentGroupId,
        IEnumerable<(LoanItemType Type, int Quantity)> items,
        DateTime loanDate)
    {
        var loan = new Loan
        {
            DroneId = droneId,
            StudentGroupId = studentGroupId,
            LoanDate = loanDate,
            Status = LoanStatus.Active
        };

        foreach (var (type, quantity) in items)
        {
            if (!Enum.IsDefined(type))
            {
                throw new ValidationException($"The accessory type \"{type}\" is not allowed. Allowed types are Battery and Propeller.");
            }

            if (quantity < 0)
            {
                throw new ValidationException($"The quantity of {type} must not be negative.");
            }

            if (loan._items.Any(item => item.Type == type))
            {
                throw new ValidationException($"The accessory type {type} must appear at most once per loan.");
            }

            loan._items.Add(new LoanItem(type, quantity));
        }

        return loan;
    }

    // Only status and return date change; drone, group, loan date and items stay as they were.
    public void Return(DateTime returnDate)
    {
        if (Status != LoanStatus.Active)
        {
            throw new ConflictException($"Loan {Id} has already been returned.");
        }

        Status = LoanStatus.Returned;
        ReturnDate = returnDate;
    }

    // A type that was not given counts as quantity 0.
    public int QuantityOf(LoanItemType type) =>
        _items.SingleOrDefault(item => item.Type == type)?.Quantity ?? 0;
}
