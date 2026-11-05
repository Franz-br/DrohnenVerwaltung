using DroneInventory.Api.Domain;

namespace DroneInventory.Api.Tests;

public class LoanTests
{
    private const int Tello07Id = 7;
    private const int Group3Id = 3;
    private static readonly DateTime LoanDate = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_with_4_batteries_and_8_propellers_records_both_quantities()
    {
        var loan = Loan.Create(Tello07Id, Group3Id, [(LoanItemType.Battery, 4), (LoanItemType.Propeller, 8)], LoanDate);

        Assert.Equal(Tello07Id, loan.DroneId);
        Assert.Equal(Group3Id, loan.StudentGroupId);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(LoanDate, loan.LoanDate);
        Assert.Null(loan.ReturnDate);
        Assert.Equal(2, loan.Items.Count);
        Assert.Equal(4, loan.QuantityOf(LoanItemType.Battery));
        Assert.Equal(8, loan.QuantityOf(LoanItemType.Propeller));
    }

    [Fact]
    public void Create_without_accessories_records_0_batteries_and_0_propellers()
    {
        var loan = Loan.Create(Tello07Id, Group3Id, [], LoanDate);

        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Empty(loan.Items);
        Assert.Equal(0, loan.QuantityOf(LoanItemType.Battery));
        Assert.Equal(0, loan.QuantityOf(LoanItemType.Propeller));
    }

    [Fact]
    public void Create_allows_a_quantity_of_0()
    {
        var loan = Loan.Create(Tello07Id, Group3Id, [(LoanItemType.Battery, 0)], LoanDate);

        Assert.Equal(0, loan.QuantityOf(LoanItemType.Battery));
    }

    [Fact]
    public void Create_with_a_negative_quantity_is_rejected()
    {
        Assert.Throws<ValidationException>(() =>
            Loan.Create(Tello07Id, Group3Id, [(LoanItemType.Battery, -1)], LoanDate));
    }

    [Fact]
    public void Create_with_an_unknown_accessory_type_is_rejected()
    {
        var charger = (LoanItemType)99;

        Assert.Throws<ValidationException>(() =>
            Loan.Create(Tello07Id, Group3Id, [(charger, 1)], LoanDate));
    }

    [Fact]
    public void Create_with_a_duplicate_accessory_type_is_rejected()
    {
        Assert.Throws<ValidationException>(() =>
            Loan.Create(Tello07Id, Group3Id, [(LoanItemType.Battery, 4), (LoanItemType.Battery, 2)], LoanDate));
    }

    [Fact]
    public void Return_marks_the_loan_as_returned_and_records_the_return_date()
    {
        var loan = Loan.Create(Tello07Id, Group3Id, [(LoanItemType.Battery, 4), (LoanItemType.Propeller, 8)], LoanDate);
        var returnDate = LoanDate.AddDays(2);

        loan.Return(returnDate);

        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(returnDate, loan.ReturnDate);
        Assert.Equal(LoanDate, loan.LoanDate);
        Assert.Equal(4, loan.QuantityOf(LoanItemType.Battery));
        Assert.Equal(8, loan.QuantityOf(LoanItemType.Propeller));
    }

    [Fact]
    public void Return_of_a_returned_loan_is_rejected_and_keeps_the_original_return_date()
    {
        var loan = Loan.Create(Tello07Id, Group3Id, [], LoanDate);
        var firstReturnDate = LoanDate.AddDays(2);
        loan.Return(firstReturnDate);

        Assert.Throws<ConflictException>(() => loan.Return(LoanDate.AddDays(5)));
        Assert.Equal(firstReturnDate, loan.ReturnDate);
    }
}
