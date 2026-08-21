using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetFinancialPosition;

/// <summary>Verifies the balance-sheet identity Assets = Liabilities + Equity + RetainedSurplusDeficit,
/// which must hold structurally because every ledger posting balances (see
/// <c>LedgerPosting.Create</c>) — this handler doesn't enforce it, it falls out of the math.</summary>
public class GetFinancialPositionQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetFinancialPositionQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetFinancialPositionQueryHandler CreateHandler() => new(_reports, _currentUser, _clock);

    [Fact]
    public async Task Assets_Equal_Liabilities_Plus_Equity_Plus_Surplus()
    {
        List<TrialBalanceAccountLine> lines =
        [
            new(new ChartOfAccountId(Guid.NewGuid()), "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit, 15_000m, 5_000m), // net 10,000
            new(new ChartOfAccountId(Guid.NewGuid()), "1100", "Resident Receivable", AccountCategory.Asset, LedgerDirection.Debit, 4_000m, 1_000m), // net 3,000
            new(new ChartOfAccountId(Guid.NewGuid()), "2000", "Accounts Payable", AccountCategory.Liability, LedgerDirection.Credit, 500m, 2_500m), // net 2,000
            new(new ChartOfAccountId(Guid.NewGuid()), "3000", "Opening Balance Equity", AccountCategory.Equity, LedgerDirection.Credit, 0m, 3_000m), // net 3,000
            new(new ChartOfAccountId(Guid.NewGuid()), "4000", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit, 0m, 12_000m), // net 12,000
            new(new ChartOfAccountId(Guid.NewGuid()), "5000", "Operating Expense", AccountCategory.Expense, LedgerDirection.Debit, 4_000m, 0m), // net 4,000
        ];
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(lines);

        FinancialPositionReportDto result = await CreateHandler().Handle(new GetFinancialPositionQuery(null), CancellationToken.None);

        result.Assets.Total.Should().Be(13_000m); // 10,000 + 3,000
        result.Liabilities.Total.Should().Be(2_000m);
        result.Equity.Total.Should().Be(3_000m);
        result.RetainedSurplusDeficit.Should().Be(8_000m); // 12,000 income - 4,000 expense
        result.TotalLiabilitiesAndEquity.Should().Be(result.Assets.Total);
    }

    [Fact]
    public async Task Income_And_Expense_Are_Never_Included_In_Any_Balance_Sheet_Section()
    {
        List<TrialBalanceAccountLine> lines =
        [
            new(new ChartOfAccountId(Guid.NewGuid()), "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit, 10_000m, 0m),
            new(new ChartOfAccountId(Guid.NewGuid()), "4000", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit, 0m, 12_000m),
            new(new ChartOfAccountId(Guid.NewGuid()), "5000", "Operating Expense", AccountCategory.Expense, LedgerDirection.Debit, 4_000m, 0m),
        ];
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(lines);

        FinancialPositionReportDto result = await CreateHandler().Handle(new GetFinancialPositionQuery(null), CancellationToken.None);

        // Only the Asset line (10,000) appears anywhere in Assets/Liabilities/Equity — Income and
        // Expense contribute solely to RetainedSurplusDeficit (8,000), never a second time to a section.
        result.Assets.Total.Should().Be(10_000m);
        result.Liabilities.Total.Should().Be(0m);
        result.Equity.Total.Should().Be(0m);
        result.RetainedSurplusDeficit.Should().Be(8_000m);
        result.TotalLiabilitiesAndEquity.Should().Be(8_000m);
    }

    [Fact]
    public async Task Zero_Balance_Accounts_Are_Excluded_From_Sections()
    {
        _reports.GetTrialBalanceAsync(TenantId, Today, Arg.Any<CancellationToken>()).Returns(
        [
            new TrialBalanceAccountLine(new ChartOfAccountId(Guid.NewGuid()), "1000", "Cash", AccountCategory.Asset, LedgerDirection.Debit, 100m, 100m),
        ]);

        FinancialPositionReportDto result = await CreateHandler().Handle(new GetFinancialPositionQuery(null), CancellationToken.None);

        result.Assets.Lines.Should().BeEmpty();
        result.Assets.Total.Should().Be(0m);
    }
}
