using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetIncomeExpenseReport;

public class GetIncomeExpenseReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly FromDate = new(2026, 8, 1);
    private static readonly DateOnly ToDate = new(2026, 8, 31);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetIncomeExpenseReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetIncomeExpenseReportQueryHandler CreateHandler() => new(_reports, _currentUser, _clock);

    [Fact]
    public async Task Surplus_Deficit_Is_Income_Minus_Expense_Never_Cash_Derived()
    {
        _reports.GetCategoryActivityAsync(TenantId, AccountCategory.Income, FromDate, ToDate, Arg.Any<CancellationToken>()).Returns(
        [
            new(new ChartOfAccountId(Guid.NewGuid()), "4000", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit, 0m, 100_000m),
        ]);
        _reports.GetCategoryActivityAsync(TenantId, AccountCategory.Expense, FromDate, ToDate, Arg.Any<CancellationToken>()).Returns(
        [
            new(new ChartOfAccountId(Guid.NewGuid()), "5000", "Operating Expense", AccountCategory.Expense, LedgerDirection.Debit, 60_000m, 0m),
        ]);

        IncomeExpenseReportDto result = await CreateHandler().Handle(new GetIncomeExpenseReportQuery(FromDate, ToDate), CancellationToken.None);

        result.TotalIncome.Should().Be(100_000m);
        result.TotalExpense.Should().Be(60_000m);
        result.SurplusDeficit.Should().Be(40_000m);
    }

    [Fact]
    public async Task Account_With_Zero_Net_Period_Activity_Is_Omitted()
    {
        _reports.GetCategoryActivityAsync(TenantId, AccountCategory.Income, FromDate, ToDate, Arg.Any<CancellationToken>()).Returns(
        [
            new(new ChartOfAccountId(Guid.NewGuid()), "4000", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit, 500m, 500m),
        ]);
        _reports.GetCategoryActivityAsync(TenantId, AccountCategory.Expense, FromDate, ToDate, Arg.Any<CancellationToken>()).Returns([]);

        IncomeExpenseReportDto result = await CreateHandler().Handle(new GetIncomeExpenseReportQuery(FromDate, ToDate), CancellationToken.None);

        result.IncomeLines.Should().BeEmpty();
        result.TotalIncome.Should().Be(0m);
        result.SurplusDeficit.Should().Be(0m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetIncomeExpenseReportQuery(FromDate, ToDate), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
