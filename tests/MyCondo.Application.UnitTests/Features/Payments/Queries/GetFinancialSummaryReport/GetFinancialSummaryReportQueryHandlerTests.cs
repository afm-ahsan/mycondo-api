using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetFinancialSummaryReport;

public class GetFinancialSummaryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetFinancialSummaryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _invoices.GetFinancialAggregateAsync(
                Arg.Any<Guid>(), Arg.Any<BuildingId?>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new InvoiceFinancialAggregate(0m, 0m, 0, 0, 0));
        _payments.GetTotalCollectedAsync(
                Arg.Any<Guid>(), Arg.Any<BuildingId?>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(0m);
    }

    private GetFinancialSummaryReportQueryHandler CreateHandler() => new(_invoices, _payments, _currentUser, _clock);

    [Fact]
    public async Task Combines_Invoice_Aggregate_And_Collected_Total_Into_The_Dto()
    {
        DateOnly fromDate = new(2026, 8, 1);
        DateOnly toDate = new(2026, 8, 31);
        InvoiceFinancialAggregate aggregate = new(
            TotalBilled: 50_000m, TotalOutstanding: 12_000m, UnpaidInvoiceCount: 3, PartiallyPaidInvoiceCount: 1, OverdueInvoiceCount: 2);

        _invoices.GetFinancialAggregateAsync(TenantId, null, fromDate, toDate, Today, Arg.Any<CancellationToken>())
            .Returns(aggregate);
        _payments.GetTotalCollectedAsync(TenantId, null, fromDate, toDate, Arg.Any<CancellationToken>())
            .Returns(38_500m);

        FinancialSummaryDto result = await CreateHandler().Handle(
            new GetFinancialSummaryReportQuery(null, fromDate, toDate), CancellationToken.None);

        result.FromDate.Should().Be(fromDate);
        result.ToDate.Should().Be(toDate);
        result.AsOfDate.Should().Be(Today);
        result.TotalBilled.Should().Be(50_000m);
        result.TotalCollected.Should().Be(38_500m);
        result.TotalOutstanding.Should().Be(12_000m);
        result.UnpaidInvoiceCount.Should().Be(3);
        result.PartiallyPaidInvoiceCount.Should().Be(1);
        result.OverdueInvoiceCount.Should().Be(2);
    }

    [Fact]
    public async Task Building_Filter_Is_Converted_And_Passed_To_Both_Repositories()
    {
        Guid rawBuildingId = Guid.NewGuid();
        DateOnly fromDate = new(2026, 8, 1);
        DateOnly toDate = new(2026, 8, 31);

        await CreateHandler().Handle(new GetFinancialSummaryReportQuery(rawBuildingId, fromDate, toDate), CancellationToken.None);

        BuildingId expected = new(rawBuildingId);
        await _invoices.Received(1).GetFinancialAggregateAsync(TenantId, expected, fromDate, toDate, Today, Arg.Any<CancellationToken>());
        await _payments.Received(1).GetTotalCollectedAsync(TenantId, expected, fromDate, toDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_Building_Filter_Passes_Null_To_Both_Repositories()
    {
        DateOnly fromDate = new(2026, 8, 1);
        DateOnly toDate = new(2026, 8, 31);

        await CreateHandler().Handle(new GetFinancialSummaryReportQuery(null, fromDate, toDate), CancellationToken.None);

        await _invoices.Received(1).GetFinancialAggregateAsync(TenantId, null, fromDate, toDate, Today, Arg.Any<CancellationToken>());
        await _payments.Received(1).GetTotalCollectedAsync(TenantId, null, fromDate, toDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetFinancialSummaryReportQuery(null, Today, Today), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
