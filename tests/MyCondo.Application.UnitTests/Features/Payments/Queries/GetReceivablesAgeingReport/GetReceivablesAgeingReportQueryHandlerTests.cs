using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetReceivablesAgeingReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetReceivablesAgeingReport;

public class GetReceivablesAgeingReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetReceivablesAgeingReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetReceivablesAgeingReportQueryHandler CreateHandler() => new(_invoices, _currentUser, _clock);

    [Fact]
    public async Task Buckets_Receivables_By_Days_Overdue_Using_Remaining_Balance()
    {
        List<AgeingReceivableLine> receivables =
        [
            new(Today, 100m),                    // due today -> Current
            new(Today.AddDays(10), 50m),          // not yet due -> Current
            new(Today.AddDays(-15), 200m),        // 15 days overdue -> 1-30
            new(Today.AddDays(-30), 300m),        // exactly 30 -> 1-30 (inclusive)
            new(Today.AddDays(-31), 400m),        // exactly 31 -> 31-60 (inclusive)
            new(Today.AddDays(-60), 500m),        // exactly 60 -> 31-60 (inclusive)
            new(Today.AddDays(-61), 600m),        // exactly 61 -> 61-90
            new(Today.AddDays(-90), 700m),        // exactly 90 -> 61-90 (inclusive)
            new(Today.AddDays(-91), 800m),        // exactly 91 -> 91+
            new(Today.AddDays(-400), 900m),       // far overdue -> 91+
        ];

        _invoices.GetOpenReceivablesAsync(TenantId, null, Arg.Any<CancellationToken>()).Returns(receivables);

        ReceivablesAgeingReportDto result = await CreateHandler().Handle(
            new GetReceivablesAgeingReportQuery(null, null), CancellationToken.None);

        result.AsOfDate.Should().Be(Today);
        result.Buckets.Should().HaveCount(5);

        AgeingBucketDto current = result.Buckets.Single(b => b.BucketLabel == "Current");
        current.InvoiceCount.Should().Be(2);
        current.TotalBalance.Should().Be(150m);

        AgeingBucketDto bucket1To30 = result.Buckets.Single(b => b.BucketLabel == "1-30 days");
        bucket1To30.InvoiceCount.Should().Be(2);
        bucket1To30.TotalBalance.Should().Be(500m);

        AgeingBucketDto bucket31To60 = result.Buckets.Single(b => b.BucketLabel == "31-60 days");
        bucket31To60.InvoiceCount.Should().Be(2);
        bucket31To60.TotalBalance.Should().Be(900m);

        AgeingBucketDto bucket61To90 = result.Buckets.Single(b => b.BucketLabel == "61-90 days");
        bucket61To90.InvoiceCount.Should().Be(2);
        bucket61To90.TotalBalance.Should().Be(1300m);

        AgeingBucketDto bucket91Plus = result.Buckets.Single(b => b.BucketLabel == "91+ days");
        bucket91Plus.InvoiceCount.Should().Be(2);
        bucket91Plus.TotalBalance.Should().Be(1700m);

        result.GrandTotal.Should().Be(150m + 500m + 900m + 1300m + 1700m);
    }

    [Fact]
    public async Task No_Open_Receivables_Returns_Five_Zero_Buckets_Not_An_Empty_List()
    {
        _invoices.GetOpenReceivablesAsync(TenantId, null, Arg.Any<CancellationToken>()).Returns([]);

        ReceivablesAgeingReportDto result = await CreateHandler().Handle(
            new GetReceivablesAgeingReportQuery(null, null), CancellationToken.None);

        result.Buckets.Should().HaveCount(5);
        result.Buckets.Should().OnlyContain(b => b.InvoiceCount == 0 && b.TotalBalance == 0m);
        result.GrandTotal.Should().Be(0m);
    }

    [Fact]
    public async Task Explicit_AsOfDate_Overrides_The_Clock()
    {
        DateOnly explicitAsOfDate = new(2026, 1, 1);
        _invoices.GetOpenReceivablesAsync(TenantId, null, Arg.Any<CancellationToken>()).Returns([]);

        ReceivablesAgeingReportDto result = await CreateHandler().Handle(
            new GetReceivablesAgeingReportQuery(null, explicitAsOfDate), CancellationToken.None);

        result.AsOfDate.Should().Be(explicitAsOfDate);
    }

    [Fact]
    public async Task Omitted_AsOfDate_Defaults_To_Todays_Business_Date()
    {
        _invoices.GetOpenReceivablesAsync(TenantId, null, Arg.Any<CancellationToken>()).Returns([]);

        ReceivablesAgeingReportDto result = await CreateHandler().Handle(
            new GetReceivablesAgeingReportQuery(null, null), CancellationToken.None);

        result.AsOfDate.Should().Be(Today);
    }

    [Fact]
    public async Task Building_Filter_Is_Converted_And_Passed_To_The_Repository()
    {
        Guid rawBuildingId = Guid.NewGuid();
        _invoices.GetOpenReceivablesAsync(TenantId, new BuildingId(rawBuildingId), Arg.Any<CancellationToken>()).Returns([]);

        await CreateHandler().Handle(new GetReceivablesAgeingReportQuery(rawBuildingId, null), CancellationToken.None);

        await _invoices.Received(1).GetOpenReceivablesAsync(TenantId, new BuildingId(rawBuildingId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetReceivablesAgeingReportQuery(null, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
