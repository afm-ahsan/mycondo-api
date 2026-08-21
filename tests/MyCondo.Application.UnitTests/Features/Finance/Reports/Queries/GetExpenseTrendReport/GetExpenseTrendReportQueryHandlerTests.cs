using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetExpenseTrendReport;

public class GetExpenseTrendReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly From = new(2026, 6, 1);
    private static readonly DateOnly To = new(2026, 8, 31);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetExpenseTrendReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetExpenseTrendReportQueryHandler CreateHandler() => new(_reports, _currentUser, _clock);

    [Fact]
    public async Task Months_Are_Netted_Debit_Minus_Credit_And_Total_Sums_All_Months()
    {
        Guid accountId = Guid.NewGuid();
        _reports.GetChartOfAccountIdForPostingRoleAsync(TenantId, nameof(LedgerAccountType.OperatingExpense), Arg.Any<CancellationToken>())
            .Returns(accountId);

        _reports.GetAccountMonthlyActivityAsync(TenantId, accountId, From, To, Arg.Any<CancellationToken>())
            .Returns(
            [
                new MonthlyAccountActivityLine(2026, 6, TotalDebit: 3_000m, TotalCredit: 0m),
                new MonthlyAccountActivityLine(2026, 7, TotalDebit: 5_000m, TotalCredit: 500m), // a partial void that month
                new MonthlyAccountActivityLine(2026, 8, TotalDebit: 4_000m, TotalCredit: 0m),
            ]);

        ExpenseTrendReportDto result = await CreateHandler().Handle(new GetExpenseTrendReportQuery(From, To), CancellationToken.None);

        result.Months.Should().HaveCount(3);
        result.Months.Single(m => m.Month == 7).TotalAmount.Should().Be(4_500m);
        result.Total.Should().Be(3_000m + 4_500m + 4_000m);
    }

    [Fact]
    public async Task No_Account_Mapping_Yet_Returns_Empty_Trend_Not_An_Error()
    {
        _reports.GetChartOfAccountIdForPostingRoleAsync(TenantId, nameof(LedgerAccountType.OperatingExpense), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        ExpenseTrendReportDto result = await CreateHandler().Handle(new GetExpenseTrendReportQuery(From, To), CancellationToken.None);

        result.Months.Should().BeEmpty();
        result.Total.Should().Be(0m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetExpenseTrendReportQuery(From, To), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
