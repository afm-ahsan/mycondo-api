using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetGasCollectionReport;

public class GetGasCollectionReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly FromDate = new(2026, 8, 1);
    private static readonly DateOnly ToDate = new(2026, 8, 31);

    private readonly IFinanceReportRepository _reports = Substitute.For<IFinanceReportRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetGasCollectionReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetGasCollectionReportQueryHandler CreateHandler() => new(_reports, _currentUser, _clock);

    [Fact]
    public async Task Queries_Only_The_GasRecoveryIncome_Account_Type()
    {
        _reports.GetIncomeCollectionAsync(TenantId, LedgerAccountType.GasRecoveryIncome, FromDate, ToDate, Arg.Any<CancellationToken>())
            .Returns(new IncomeCollectionSummary(Billed: 30_000m, BilledInvoiceCount: 12, Collected: 25_000m, Waived: 0m));

        GasCollectionReportDto result = await CreateHandler().Handle(new GetGasCollectionReportQuery(FromDate, ToDate), CancellationToken.None);

        result.Billed.Should().Be(30_000m);
        result.Collected.Should().Be(25_000m);
        await _reports.Received(1).GetIncomeCollectionAsync(
            TenantId, LedgerAccountType.GasRecoveryIncome, FromDate, ToDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetGasCollectionReportQuery(FromDate, ToDate), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
