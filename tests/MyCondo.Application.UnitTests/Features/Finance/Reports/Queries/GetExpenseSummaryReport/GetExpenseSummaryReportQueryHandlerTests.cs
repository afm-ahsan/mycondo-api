using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetExpenseSummaryReport;

public class GetExpenseSummaryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly From = new(2026, 8, 1);
    private static readonly DateOnly To = new(2026, 8, 31);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetExpenseSummaryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetExpenseSummaryReportQueryHandler CreateHandler() => new(_reports, _expenses, _currentUser, _clock);

    [Fact]
    public async Task Ledger_Total_Nets_Debit_Minus_Credit_Across_Expense_Category_Accounts()
    {
        _reports.GetCategoryActivityAsync(TenantId, AccountCategory.Expense, From, To, Arg.Any<CancellationToken>())
            .Returns(
            [
                new TrialBalanceAccountLine(
                    new ChartOfAccountId(Guid.NewGuid()), "5000", "Operating Expenses", AccountCategory.Expense,
                    LedgerDirection.Debit, TotalDebit: 12_000m, TotalCredit: 2_000m), // net 10,000
            ]);

        _expenses.GetStatusTotalsForPeriodAsync(TenantId, From, To, Arg.Any<CancellationToken>())
            .Returns(
            [
                new ExpenseStatusTotal(ExpenseStatus.Posted, 3, 6_000m),
                new ExpenseStatusTotal(ExpenseStatus.Paid, 2, 4_000m),
                new ExpenseStatusTotal(ExpenseStatus.Recorded, 1, 1_500m),
            ]);

        ExpenseSummaryReportDto result = await CreateHandler().Handle(
            new GetExpenseSummaryReportQuery(From, To), CancellationToken.None);

        result.LedgerTotal.Should().Be(10_000m);
        result.SourceRecordTotal.Should().Be(10_000m); // 6,000 (Posted) + 4,000 (Paid) — Recorded excluded
        result.IsReconciled.Should().BeTrue();
        result.ByStatus.Should().HaveCount(3);
    }

    [Fact]
    public async Task Mismatched_Ledger_And_Source_Totals_Are_Surfaced_Not_Hidden()
    {
        // A later-period void means the current-snapshot source total legitimately diverges from the
        // period's ledger total — the report must expose this, not silently reconcile it away.
        _reports.GetCategoryActivityAsync(TenantId, AccountCategory.Expense, From, To, Arg.Any<CancellationToken>())
            .Returns(
            [
                new TrialBalanceAccountLine(
                    new ChartOfAccountId(Guid.NewGuid()), "5000", "Operating Expenses", AccountCategory.Expense,
                    LedgerDirection.Debit, TotalDebit: 5_000m, TotalCredit: 0m),
            ]);

        _expenses.GetStatusTotalsForPeriodAsync(TenantId, From, To, Arg.Any<CancellationToken>())
            .Returns([new ExpenseStatusTotal(ExpenseStatus.Voided, 1, 5_000m)]);

        ExpenseSummaryReportDto result = await CreateHandler().Handle(
            new GetExpenseSummaryReportQuery(From, To), CancellationToken.None);

        result.LedgerTotal.Should().Be(5_000m);
        result.SourceRecordTotal.Should().Be(0m); // Voided excluded from source-record total
        result.IsReconciled.Should().BeFalse();
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetExpenseSummaryReportQuery(From, To), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
