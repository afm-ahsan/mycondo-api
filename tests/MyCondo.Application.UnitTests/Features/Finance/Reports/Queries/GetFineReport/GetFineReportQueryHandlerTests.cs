using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFineReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetFineReport;

public class GetFineReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly FromDate = new(2026, 8, 1);
    private static readonly DateOnly ToDate = new(2026, 8, 31);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetFineReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetFineReportQueryHandler CreateHandler() => new(_reports, _currentUser, _clock);

    [Fact]
    public async Task Waived_Fines_Are_Reported_Separately_From_Collected()
    {
        _reports.GetIncomeCollectionAsync(TenantId, LedgerAccountType.FineIncome, FromDate, ToDate, Arg.Any<CancellationToken>())
            .Returns(new IncomeCollectionSummary(Billed: 10_000m, BilledInvoiceCount: 5, Collected: 4_000m, Waived: 3_000m));

        FineReportDto result = await CreateHandler().Handle(new GetFineReportQuery(FromDate, ToDate), CancellationToken.None);

        result.Billed.Should().Be(10_000m);
        result.Collected.Should().Be(4_000m);
        result.Waived.Should().Be(3_000m);
    }

    [Fact]
    public async Task Queries_Only_The_FineIncome_Account_Type()
    {
        _reports.GetIncomeCollectionAsync(TenantId, LedgerAccountType.FineIncome, FromDate, ToDate, Arg.Any<CancellationToken>())
            .Returns(new IncomeCollectionSummary(0m, 0, 0m, 0m));

        await CreateHandler().Handle(new GetFineReportQuery(FromDate, ToDate), CancellationToken.None);

        await _reports.Received(1).GetIncomeCollectionAsync(
            TenantId, LedgerAccountType.FineIncome, FromDate, ToDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetFineReportQuery(FromDate, ToDate), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
