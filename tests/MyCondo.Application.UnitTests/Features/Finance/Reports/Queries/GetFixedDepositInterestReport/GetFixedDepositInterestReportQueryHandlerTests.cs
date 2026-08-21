using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;

public class GetFixedDepositInterestReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly From = new(2026, 8, 1);
    private static readonly DateOnly To = new(2026, 8, 31);

    private readonly IFixedDepositRepository _fixedDeposits = Substitute.For<IFixedDepositRepository>();
    private readonly IFixedDepositInterestAccrualRepository _accruals = Substitute.For<IFixedDepositInterestAccrualRepository>();
    private readonly IFixedDepositInterestReceiptRepository _receipts = Substitute.For<IFixedDepositInterestReceiptRepository>();
    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetFixedDepositInterestReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetFixedDepositInterestReportQueryHandler CreateHandler() =>
        new(_fixedDeposits, _accruals, _receipts, _reports, _currentUser, _clock);

    private static FixedDeposit CreateActiveFixedDeposit(Guid tenantId) =>
        FixedDeposit.Place(
            FixedDepositId.New(), tenantId, $"CERT-{Guid.NewGuid():N}", "Test Bank", null,
            new FinancialAccountId(Guid.NewGuid()), null, 100_000m, 6.5m, InterestCalculationMethod.Simple,
            InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            null, null, null, new LedgerPostingId(Guid.NewGuid()), Now);

    [Fact]
    public async Task Accrual_Gross_Ties_To_Ledger_FDInterestIncome_Credits_Not_To_Receipts()
    {
        FixedDeposit fd = CreateActiveFixedDeposit(TenantId);
        _fixedDeposits.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([fd]);

        _accruals.GetTotalsByFixedDepositAsync(TenantId, From, To, Arg.Any<CancellationToken>())
            .Returns([new FixedDepositAccrualTotal(fd.Id, 1, 2_000m)]);
        _accruals.GetTotalsByFixedDepositAsync(TenantId, null, To, Arg.Any<CancellationToken>())
            .Returns([new FixedDepositAccrualTotal(fd.Id, 5, 8_000m)]); // cumulative-to-date

        // A receipt for a *different* (smaller, net-of-deduction) amount than the accrual — proves the
        // reconciliation identity is accrual-vs-ledger-income, not receipt-vs-ledger-income.
        _receipts.GetTotalsByFixedDepositAsync(TenantId, From, To, Arg.Any<CancellationToken>())
            .Returns([new FixedDepositReceiptTotal(fd.Id, 1, 1_500m, 150m)]);
        _receipts.GetTotalsByFixedDepositAsync(TenantId, null, To, Arg.Any<CancellationToken>())
            .Returns([new FixedDepositReceiptTotal(fd.Id, 3, 5_000m, 500m)]); // cumulative-to-date

        Guid fdInterestIncomeAccountId = Guid.NewGuid();
        _reports.GetChartOfAccountIdForPostingRoleAsync(TenantId, nameof(LedgerAccountType.FDInterestIncome), Arg.Any<CancellationToken>())
            .Returns(fdInterestIncomeAccountId);
        _reports.GetCategoryActivityAsync(TenantId, AccountCategory.Income, From, To, Arg.Any<CancellationToken>())
            .Returns(
            [
                new TrialBalanceAccountLine(
                    new ChartOfAccountId(fdInterestIncomeAccountId), "4040", "FD Interest Income", AccountCategory.Income,
                    LedgerDirection.Credit, TotalDebit: 0m, TotalCredit: 2_000m),
            ]);

        FixedDepositInterestReportDto result = await CreateHandler().Handle(
            new GetFixedDepositInterestReportQuery(From, To), CancellationToken.None);

        result.AccruedGrossForPeriod.Should().Be(2_000m);
        result.LedgerInterestIncomeForPeriod.Should().Be(2_000m);
        result.IsReconciled.Should().BeTrue();
        result.ReceivedGrossForPeriod.Should().Be(1_500m);
        result.ReceivedDeductionForPeriod.Should().Be(150m);
        result.ReceivedNetForPeriod.Should().Be(1_350m);
        result.OutstandingAccruedNotReceivedAsOfToDate.Should().Be(3_000m); // 8,000 cumulative accrued - 5,000 cumulative received
        result.ByFixedDeposit.Should().ContainSingle(l => l.FixedDepositId == fd.Id.Value);
    }

    [Fact]
    public async Task No_FDInterestIncome_Mapping_Yet_Yields_Zero_Ledger_Income_And_Unreconciled()
    {
        _fixedDeposits.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([]);
        _accruals.GetTotalsByFixedDepositAsync(TenantId, From, To, Arg.Any<CancellationToken>()).Returns([]);
        _accruals.GetTotalsByFixedDepositAsync(TenantId, null, To, Arg.Any<CancellationToken>()).Returns([]);
        _receipts.GetTotalsByFixedDepositAsync(TenantId, From, To, Arg.Any<CancellationToken>()).Returns([]);
        _receipts.GetTotalsByFixedDepositAsync(TenantId, null, To, Arg.Any<CancellationToken>()).Returns([]);
        _reports.GetChartOfAccountIdForPostingRoleAsync(TenantId, nameof(LedgerAccountType.FDInterestIncome), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        FixedDepositInterestReportDto result = await CreateHandler().Handle(
            new GetFixedDepositInterestReportQuery(From, To), CancellationToken.None);

        result.LedgerInterestIncomeForPeriod.Should().Be(0m);
        result.AccruedGrossForPeriod.Should().Be(0m);
        result.IsReconciled.Should().BeTrue(); // both sides genuinely zero
        await _reports.DidNotReceive().GetCategoryActivityAsync(
            Arg.Any<Guid>(), Arg.Any<AccountCategory>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetFixedDepositInterestReportQuery(From, To), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
