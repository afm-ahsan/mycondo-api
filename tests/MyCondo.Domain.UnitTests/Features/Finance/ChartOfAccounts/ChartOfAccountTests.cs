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

    [Fact]
    public void Create_Accepts_A_StatementGroup_Matching_The_Account_Category()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.CashAndBank);

        account.StatementGroup.Should().Be(FinancialStatementGroup.CashAndBank);
        account.EffectiveStatementGroup.Should().Be(FinancialStatementGroup.CashAndBank);
    }

    [Fact]
    public void Create_Throws_When_StatementGroup_Belongs_To_A_Different_Category()
    {
        Action act = () => ChartOfAccount.Create(
            TenantId, "2300", "Accounts Payable", AccountCategory.Liability, LedgerDirection.Credit,
            statementGroup: FinancialStatementGroup.CashAndBank);

        act.Should().Throw<StatementGroupCategoryMismatchException>();
    }

    [Fact]
    public void EffectiveStatementGroup_Falls_Back_To_The_Category_Default_When_Unclassified()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "5900", "Miscellaneous", AccountCategory.Expense, LedgerDirection.Debit);

        account.StatementGroup.Should().BeNull();
        account.EffectiveStatementGroup.Should().Be(FinancialStatementGroup.OtherExpenses);
    }

    [Fact]
    public void Reclassify_Updates_The_StatementGroup()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "1400", "MFS Wallet", AccountCategory.Asset, LedgerDirection.Debit);

        account.Reclassify(FinancialStatementGroup.CashAndBank);

        account.StatementGroup.Should().Be(FinancialStatementGroup.CashAndBank);
    }

    [Fact]
    public void Reclassify_Succeeds_For_A_System_Account()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit, isSystemAccount: true,
            statementGroup: FinancialStatementGroup.CashAndBank);

        account.Reclassify(FinancialStatementGroup.OtherAssets);

        account.StatementGroup.Should().Be(FinancialStatementGroup.OtherAssets);
    }

    [Fact]
    public void Reclassify_Throws_When_StatementGroup_Belongs_To_A_Different_Category()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit);

        Action act = () => account.Reclassify(FinancialStatementGroup.AccountsPayable);

        act.Should().Throw<StatementGroupCategoryMismatchException>();
        account.StatementGroup.Should().BeNull();
    }

    [Fact]
    public void Reclassify_Null_Clears_An_Explicit_StatementGroup()
    {
        ChartOfAccount account = ChartOfAccount.Create(
            TenantId, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.CashAndBank);

        account.Reclassify(null);

        account.StatementGroup.Should().BeNull();
        account.EffectiveStatementGroup.Should().Be(FinancialStatementGroup.OtherAssets);
    }
}
