namespace DroneInventory.Api.Domain;

// An accessory position of a loan. It only exists inside its Loan and is created by Loan.Create.
public class LoanItem
{
    private LoanItem()
    {
    }

    internal LoanItem(LoanItemType type, int quantity)
    {
        Type = type;
        Quantity = quantity;
    }

    public int Id { get; private set; }

    public int LoanId { get; private set; }

    public LoanItemType Type { get; private set; }

    public int Quantity { get; private set; }
}
