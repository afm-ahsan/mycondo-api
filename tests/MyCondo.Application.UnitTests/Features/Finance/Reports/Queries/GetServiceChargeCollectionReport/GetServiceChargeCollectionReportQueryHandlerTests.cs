using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;

public class GetServiceChargeCollectionReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly FromDate = new(2026, 8, 1);
    private static readonly DateOnly ToDate = new(2026, 8, 31);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetServiceChargeCollectionReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetServiceChargeCollectionReportQueryHandler CreateHandler() => new(_reports, _currentUser, _clock);

    [Fact]
    public async Task Billed_And_Collected_Are_Reported_As_Separate_Figures_Not_Merged()
    {
        _reports.GetIncomeCollectionAsync(TenantId, LedgerAccountType.ServiceChargeIncome, FromDate, ToDate, Arg.Any<CancellationToken>())
            .Returns(new IncomeCollectionSummary(Billed: 200_000m, BilledInvoiceCount: 40, Collected: 150_000m, Waived: 0m));

        ServiceChargeCollectionReportDto result = await CreateHandler().Handle(
            new GetServiceChargeCollectionReportQuery(FromDate, ToDate), CancellationToken.None);

        result.Billed.Should().Be(200_000m);
        result.Collected.Should().Be(150_000m);
        result.Billed.Should().NotBe(result.Collected); // sanity: never silently reconciled/merged
        result.BilledInvoiceCount.Should().Be(40);
    }

    [Fact]
    public async Task Queries_Only_The_ServiceChargeIncome_Account_Type()
    {
        _reports.GetIncomeCollectionAsync(TenantId, LedgerAccountType.ServiceChargeIncome, FromDate, ToDate, Arg.Any<CancellationToken>())
            .Returns(new IncomeCollectionSummary(0m, 0, 0m, 0m));

        await CreateHandler().Handle(new GetServiceChargeCollectionReportQuery(FromDate, ToDate), CancellationToken.None);

        await _reports.Received(1).GetIncomeCollectionAsync(
            TenantId, LedgerAccountType.ServiceChargeIncome, FromDate, ToDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetServiceChargeCollectionReportQuery(FromDate, ToDate), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
