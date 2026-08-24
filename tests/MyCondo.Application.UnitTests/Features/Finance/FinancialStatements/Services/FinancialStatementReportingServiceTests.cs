using AwesomeAssertions;
using MyCondo.Application.Features.Finance.FinancialStatements.Services;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Services;

public class FinancialStatementReportingServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly AsOfDate = new(2026, 6, 30);
    private static readonly DateOnly StartDate = new(2026, 6, 1);
    private static readonly DateOnly EndDate = new(2026, 6, 30);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();

    private FinancialStatementReportingService CreateService() => new(_reports);

    private static StatementAccountActivityLine Line(
        string code, string name, AccountCategory category, LedgerDirection normalBalance,
        FinancialStatementGroup? statementGroup, FinancialStatementGroup effectiveGroup,
        decimal totalDebit, decimal totalCredit) =>
        new(new ChartOfAccountId(Guid.NewGuid()), code, name, category, normalBalance,
            statementGroup, effectiveGroup, totalDebit, totalCredit);

    [Fact]
    public async Task Asset_Account_Nets_Debit_Minus_Credit()
    {
        _reports.GetStatementBalancesAsOfAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
                FinancialStatementGroup.CashAndBank, FinancialStatementGroup.CashAndBank, 10_000m, 3_000m),
        ]);

        FinancialStatementSnapshot snapshot = await CreateService().GetAsOfSnapshotAsync(TenantId, AsOfDate, null, CancellationToken.None);

        snapshot.AmountFor(FinancialStatementGroup.CashAndBank).Should().Be(7_000m);
        snapshot.TotalFor(AccountCategory.Asset).Should().Be(7_000m);
    }

    [Fact]
    public async Task Liability_Account_Nets_Credit_Minus_Debit()
    {
        _reports.GetStatementBalancesAsOfAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("2000", "Accounts Payable", AccountCategory.Liability, LedgerDirection.Credit,
                FinancialStatementGroup.AccountsPayable, FinancialStatementGroup.AccountsPayable, 500m, 4_500m),
        ]);

        FinancialStatementSnapshot snapshot = await CreateService().GetAsOfSnapshotAsync(TenantId, AsOfDate, null, CancellationToken.None);

        snapshot.AmountFor(FinancialStatementGroup.AccountsPayable).Should().Be(4_000m);
        snapshot.TotalFor(AccountCategory.Liability).Should().Be(4_000m);
    }

    [Fact]
    public async Task Income_Account_Nets_Credit_Minus_Debit_For_Period()
    {
        _reports.GetStatementActivityForPeriodAsync(TenantId, StartDate, EndDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("4000", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit,
                FinancialStatementGroup.ServiceChargeIncome, FinancialStatementGroup.ServiceChargeIncome, 0m, 100_000m),
        ]);

        FinancialStatementSnapshot snapshot =
            await CreateService().GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, null, CancellationToken.None);

        snapshot.AmountFor(FinancialStatementGroup.ServiceChargeIncome).Should().Be(100_000m);
        snapshot.TotalFor(AccountCategory.Income).Should().Be(100_000m);
    }

    [Fact]
    public async Task Expense_Account_Nets_Debit_Minus_Credit_For_Period()
    {
        _reports.GetStatementActivityForPeriodAsync(TenantId, StartDate, EndDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("5000", "Operating Expense", AccountCategory.Expense, LedgerDirection.Debit,
                FinancialStatementGroup.OperatingExpenses, FinancialStatementGroup.OperatingExpenses, 60_000m, 0m),
        ]);

        FinancialStatementSnapshot snapshot =
            await CreateService().GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, null, CancellationToken.None);

        snapshot.AmountFor(FinancialStatementGroup.OperatingExpenses).Should().Be(60_000m);
        snapshot.TotalFor(AccountCategory.Expense).Should().Be(60_000m);
    }

    [Fact]
    public async Task Account_With_Exactly_Offsetting_Activity_Is_Omitted()
    {
        // Represents a fully reversed posting (original + reversing entry) netting to zero.
        _reports.GetStatementActivityForPeriodAsync(TenantId, StartDate, EndDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("5000", "Operating Expense", AccountCategory.Expense, LedgerDirection.Debit,
                FinancialStatementGroup.OperatingExpenses, FinancialStatementGroup.OperatingExpenses, 60_000m, 60_000m),
        ]);

        FinancialStatementSnapshot snapshot =
            await CreateService().GetPeriodSnapshotAsync(TenantId, StartDate, EndDate, null, CancellationToken.None);

        snapshot.Groups.Should().BeEmpty();
        snapshot.UnmappedAccountWarnings.Should().BeEmpty();
    }

    [Fact]
    public async Task Unclassified_Account_With_NonZero_Balance_Rolls_Up_Under_Fallback_And_Warns()
    {
        _reports.GetStatementBalancesAsOfAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("1999", "Custom Petty Cash Box", AccountCategory.Asset, LedgerDirection.Debit,
                statementGroup: null, effectiveGroup: FinancialStatementGroup.OtherAssets, 1_500m, 0m),
        ]);

        FinancialStatementSnapshot snapshot = await CreateService().GetAsOfSnapshotAsync(TenantId, AsOfDate, null, CancellationToken.None);

        snapshot.AmountFor(FinancialStatementGroup.OtherAssets).Should().Be(1_500m);
        snapshot.UnmappedAccountWarnings.Should().ContainSingle();
        snapshot.UnmappedAccountWarnings[0].Code.Should().Be("1999");
        snapshot.UnmappedAccountWarnings[0].EffectiveStatementGroup.Should().Be(FinancialStatementGroup.OtherAssets);
        snapshot.UnmappedAccountWarnings[0].Amount.Should().Be(1_500m);
    }

    [Fact]
    public async Task Explicitly_Classified_Custom_Account_Produces_No_Warning()
    {
        // A tenant custom account that HAS been assigned a StatementGroup must not be flagged, even
        // though it isn't one of the seeded system accounts.
        _reports.GetStatementBalancesAsOfAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("1050", "MFS Wallet", AccountCategory.Asset, LedgerDirection.Debit,
                statementGroup: FinancialStatementGroup.CashAndBank, effectiveGroup: FinancialStatementGroup.CashAndBank, 2_000m, 0m),
        ]);

        FinancialStatementSnapshot snapshot = await CreateService().GetAsOfSnapshotAsync(TenantId, AsOfDate, null, CancellationToken.None);

        snapshot.UnmappedAccountWarnings.Should().BeEmpty();
        snapshot.AmountFor(FinancialStatementGroup.CashAndBank).Should().Be(2_000m);
    }

    [Fact]
    public async Task Multiple_Accounts_In_The_Same_Group_Are_Aggregated_Into_One_Group_Total()
    {
        _reports.GetStatementBalancesAsOfAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>()).Returns(
        [
            Line("1000", "Bank Account A", AccountCategory.Asset, LedgerDirection.Debit,
                FinancialStatementGroup.CashAndBank, FinancialStatementGroup.CashAndBank, 5_000m, 0m),
            Line("1010", "Bank Account B", AccountCategory.Asset, LedgerDirection.Debit,
                FinancialStatementGroup.CashAndBank, FinancialStatementGroup.CashAndBank, 3_000m, 0m),
        ]);

        FinancialStatementSnapshot snapshot = await CreateService().GetAsOfSnapshotAsync(TenantId, AsOfDate, null, CancellationToken.None);

        snapshot.Groups.Should().ContainSingle(g => g.Group == FinancialStatementGroup.CashAndBank);
        snapshot.AmountFor(FinancialStatementGroup.CashAndBank).Should().Be(8_000m);
        snapshot.Groups.Single().Accounts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Zero_Activity_Snapshot_Returns_Empty_Groups_And_No_Warnings()
    {
        _reports.GetStatementBalancesAsOfAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>()).Returns([]);

        FinancialStatementSnapshot snapshot = await CreateService().GetAsOfSnapshotAsync(TenantId, AsOfDate, null, CancellationToken.None);

        snapshot.Groups.Should().BeEmpty();
        snapshot.UnmappedAccountWarnings.Should().BeEmpty();
    }

    [Fact]
    public async Task FundId_Is_Forwarded_To_The_Repository_Unchanged()
    {
        Guid fundId = Guid.NewGuid();
        _reports.GetStatementBalancesAsOfAsync(TenantId, AsOfDate, fundId, Arg.Any<CancellationToken>()).Returns(
        [
            Line("1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
                FinancialStatementGroup.CashAndBank, FinancialStatementGroup.CashAndBank, 1_000m, 0m),
        ]);

        FinancialStatementSnapshot snapshot = await CreateService().GetAsOfSnapshotAsync(TenantId, AsOfDate, fundId, CancellationToken.None);

        snapshot.AmountFor(FinancialStatementGroup.CashAndBank).Should().Be(1_000m);
        await _reports.Received(1).GetStatementBalancesAsOfAsync(TenantId, AsOfDate, fundId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TenantId_Is_Forwarded_To_The_Repository_Unchanged()
    {
        Guid otherTenantId = Guid.NewGuid();
        _reports.GetStatementBalancesAsOfAsync(otherTenantId, AsOfDate, null, Arg.Any<CancellationToken>()).Returns([]);

        await CreateService().GetAsOfSnapshotAsync(otherTenantId, AsOfDate, null, CancellationToken.None);

        await _reports.Received(1).GetStatementBalancesAsOfAsync(otherTenantId, AsOfDate, null, Arg.Any<CancellationToken>());
        await _reports.DidNotReceive().GetStatementBalancesAsOfAsync(TenantId, AsOfDate, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndDate_Before_StartDate_Throws()
    {
        Func<Task> act = () => CreateService()
            .GetPeriodSnapshotAsync(TenantId, EndDate, StartDate, null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
