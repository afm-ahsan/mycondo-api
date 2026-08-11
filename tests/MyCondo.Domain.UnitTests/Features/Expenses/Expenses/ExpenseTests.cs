using AwesomeAssertions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.Expenses.Exceptions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.UnitTests.Features.Expenses.Expenses;

public class ExpenseTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly ExpenseTypeId ExpenseTypeId = ExpenseTypeId.New();

    private static Expense Record(decimal amount = 1000m) => Expense.Record(
        TenantId, BuildingId, ExpenseTypeId, new DateOnly(2026, 8, 1), "  Monthly cleaning  ", " ABC Cleaners ",
        " INV-001 ", amount, PaymentMethod.Cash, " paid in full ", Now);

    [Fact]
    public void Record_Trims_Text_Fields_And_Starts_Recorded()
    {
        Expense expense = Record();

        expense.Description.Should().Be("Monthly cleaning");
        expense.Payee.Should().Be("ABC Cleaners");
        expense.ReferenceNumber.Should().Be("INV-001");
        expense.Notes.Should().Be("paid in full");
        expense.Status.Should().Be(ExpenseStatus.Recorded);
        expense.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_Rejects_Non_Positive_Amounts(decimal amount)
    {
        Action act = () => Record(amount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_Changes_Fields_And_Bumps_Version()
    {
        Expense expense = Record();

        expense.Update(
            BuildingId, ExpenseTypeId, new DateOnly(2026, 8, 2), "Corrected description", "New Payee", "INV-002",
            2000m, PaymentMethod.BankTransfer, "updated", DateTimeOffset.UtcNow);

        expense.Description.Should().Be("Corrected description");
        expense.Amount.Should().Be(2000m);
        expense.PaymentMethod.Should().Be(PaymentMethod.BankTransfer);
        expense.Version.Should().Be(2);
    }

    [Fact]
    public void Update_Throws_When_Amount_Is_Not_Positive()
    {
        Expense expense = Record();

        Action act = () => expense.Update(
            BuildingId, ExpenseTypeId, new DateOnly(2026, 8, 2), "Desc", null, null, 0m, PaymentMethod.Cash, null,
            DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Void_Sets_Status_And_Reason()
    {
        Expense expense = Record();

        expense.Void("Duplicate entry", DateTimeOffset.UtcNow);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.VoidReason.Should().Be("Duplicate entry");
    }

    [Fact]
    public void Void_Is_Idempotent_For_An_Already_Voided_Expense()
    {
        Expense expense = Record();
        expense.Void("First reason", DateTimeOffset.UtcNow);
        int versionAfterFirstVoid = expense.Version;

        expense.Void("Second reason", DateTimeOffset.UtcNow);

        expense.VoidReason.Should().Be("First reason");
        expense.Version.Should().Be(versionAfterFirstVoid);
    }

    [Fact]
    public void Update_Throws_When_Expense_Is_Already_Voided()
    {
        Expense expense = Record();
        expense.Void("Reason", DateTimeOffset.UtcNow);

        Action act = () => expense.Update(
            BuildingId, ExpenseTypeId, new DateOnly(2026, 8, 2), "Desc", null, null, 500m, PaymentMethod.Cash, null,
            DateTimeOffset.UtcNow);

        act.Should().Throw<ExpenseAlreadyVoidedException>();
    }
}
