using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.ChartOfAccounts.Exceptions;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.UnitTests.Features.Finance.ChartOfAccounts;

public class ChartOfAccountTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Produces_An_Active_Non_System_Account_By_Default()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "5000", "Security", AccountCategory.Expense, LedgerDirection.Debit);

        account.TenantId.Should().Be(TenantId);
        account.Code.Should().Be("5000");
        account.IsActive.Should().BeTrue();
        account.IsSystemAccount.Should().BeFalse();
    }

    [Fact]
    public void Create_Throws_When_TenantId_Is_Empty()
    {
        Action act = () => ChartOfAccount.Create(
            Guid.Empty, "5000", "Security", AccountCategory.Expense, LedgerDirection.Debit);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Succeeds_For_A_Non_System_Account()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "5000", "Security", AccountCategory.Expense, LedgerDirection.Debit);

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Throws_For_A_System_Account()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit, isSystemAccount: true);

        Action act = () => account.Deactivate();

        act.Should().Throw<SystemAccountCannotBeModifiedException>();
        account.IsActive.Should().BeTrue();
    }
}
