using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Domain.UnitTests.Features.Finance.FinancialAccounts;

public class FinancialAccountTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly ChartOfAccountId LedgerAccountId = ChartOfAccountId.New();

    private static FinancialAccount Create() =>
        FinancialAccount.Create(
            TenantId, "  Main Bank Account  ", FinancialAccountType.Bank, " City Bank ", " Gulshan Branch ",
            " 123456789 ", LedgerAccountId, fundId: null, " primary operating account ");

    [Fact]
    public void Create_Trims_Text_Fields_And_Starts_Active()
    {
        FinancialAccount account = Create();

        account.Name.Should().Be("Main Bank Account");
        account.BankName.Should().Be("City Bank");
        account.BranchName.Should().Be("Gulshan Branch");
        account.AccountNumber.Should().Be("123456789");
        account.Notes.Should().Be("primary operating account");
        account.ChartOfAccountId.Should().Be(LedgerAccountId);
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Throws_When_Name_Is_Empty()
    {
        Action act = () => FinancialAccount.Create(
            TenantId, "", FinancialAccountType.Cash, null, null, null, LedgerAccountId, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_Changes_Fields_But_Never_ChartOfAccountId()
    {
        FinancialAccount account = Create();
        FundId fundId = FundId.New();

        account.Update("Renamed Account", "New Bank", "New Branch", "999", fundId, "updated notes");

        account.Name.Should().Be("Renamed Account");
        account.BankName.Should().Be("New Bank");
        account.FundId.Should().Be(fundId);
        account.ChartOfAccountId.Should().Be(LedgerAccountId);
    }

    [Fact]
    public void Deactivate_Then_Activate_Toggles_IsActive()
    {
        FinancialAccount account = Create();

        account.Deactivate();
        account.IsActive.Should().BeFalse();

        account.Activate();
        account.IsActive.Should().BeTrue();
    }
}
