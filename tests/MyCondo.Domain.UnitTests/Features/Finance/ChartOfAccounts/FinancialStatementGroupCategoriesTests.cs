using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Domain.UnitTests.Features.Finance.ChartOfAccounts;

public class FinancialStatementGroupCategoriesTests
{
    [Fact]
    public void CategoryOf_Is_Defined_For_Every_FinancialStatementGroup()
    {
        foreach (FinancialStatementGroup group in Enum.GetValues<FinancialStatementGroup>())
        {
            Action act = () => FinancialStatementGroupCategories.CategoryOf(group);
            act.Should().NotThrow($"every {nameof(FinancialStatementGroup)} member must map to an {nameof(AccountCategory)}");
        }
    }

    [Fact]
    public void DefaultFor_Is_Defined_For_Every_AccountCategory()
    {
        foreach (AccountCategory category in Enum.GetValues<AccountCategory>())
        {
            FinancialStatementGroup defaultGroup = FinancialStatementGroupCategories.DefaultFor(category);

            FinancialStatementGroupCategories.CategoryOf(defaultGroup).Should().Be(category,
                "the fallback group for a category must itself belong to that category, or unclassified " +
                "accounts would be misclassified");
        }
    }

    [Theory]
    [InlineData(FinancialStatementGroup.CashAndBank, AccountCategory.Asset)]
    [InlineData(FinancialStatementGroup.InvestmentsAndFixedDeposits, AccountCategory.Asset)]
    [InlineData(FinancialStatementGroup.Receivables, AccountCategory.Asset)]
    [InlineData(FinancialStatementGroup.AccruedInterestReceivable, AccountCategory.Asset)]
    [InlineData(FinancialStatementGroup.OtherAssets, AccountCategory.Asset)]
    [InlineData(FinancialStatementGroup.AccountsPayable, AccountCategory.Liability)]
    [InlineData(FinancialStatementGroup.ResidentAdvances, AccountCategory.Liability)]
    [InlineData(FinancialStatementGroup.OtherLiabilities, AccountCategory.Liability)]
    [InlineData(FinancialStatementGroup.FundBalance, AccountCategory.Equity)]
    [InlineData(FinancialStatementGroup.AccumulatedSurplus, AccountCategory.Equity)]
    [InlineData(FinancialStatementGroup.ServiceChargeIncome, AccountCategory.Income)]
    [InlineData(FinancialStatementGroup.UtilityGasIncome, AccountCategory.Income)]
    [InlineData(FinancialStatementGroup.FineIncome, AccountCategory.Income)]
    [InlineData(FinancialStatementGroup.InterestIncome, AccountCategory.Income)]
    [InlineData(FinancialStatementGroup.OtherIncome, AccountCategory.Income)]
    [InlineData(FinancialStatementGroup.OperatingExpenses, AccountCategory.Expense)]
    [InlineData(FinancialStatementGroup.InterestExpense, AccountCategory.Expense)]
    [InlineData(FinancialStatementGroup.OtherExpenses, AccountCategory.Expense)]
    public void CategoryOf_Returns_The_Expected_Category(FinancialStatementGroup group, AccountCategory expected)
    {
        FinancialStatementGroupCategories.CategoryOf(group).Should().Be(expected);
    }

    [Theory]
    [InlineData(FinancialStatementGroup.CashAndBank, AccountCategory.Asset, true)]
    [InlineData(FinancialStatementGroup.CashAndBank, AccountCategory.Liability, false)]
    [InlineData(FinancialStatementGroup.AccountsPayable, AccountCategory.Liability, true)]
    [InlineData(FinancialStatementGroup.AccountsPayable, AccountCategory.Expense, false)]
    public void IsValidFor_Reflects_The_Group_Category_Relationship(
        FinancialStatementGroup group, AccountCategory category, bool expected)
    {
        FinancialStatementGroupCategories.IsValidFor(group, category).Should().Be(expected);
    }
}
