using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.ExportConsumptionHistoryReport;
using MyCondo.Application.Features.Utilities.Queries.GetReadings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.ExportConsumptionHistoryReport;

public class ExportConsumptionHistoryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IMeterRepository _meters = Substitute.For<IMeterRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    public ExportConsumptionHistoryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private ExportConsumptionHistoryReportQueryHandler CreateHandler() =>
        new(_sender, _meters, _buildings, _currentUser, _exportService);

    private static Meter InstalledMeter(BuildingId buildingId) =>
        Meter.Install(TenantId, buildingId, UtilityType.Electricity, "MTR-001", DateTimeOffset.UtcNow);

    private static ReadingDto SampleReading(Guid meterId, DateOnly periodStart) => new(
        ReadingId: Guid.NewGuid(),
        MeterId: meterId,
        FlatId: Guid.NewGuid(),
        UtilityType: "Electricity",
        BuildingId: Guid.NewGuid(),
        PeriodStart: periodStart,
        PeriodEnd: periodStart.AddMonths(1),
        PreviousReading: 100m,
        PresentReading: 150m,
        ConsumptionUnits: 50m,
        ReadingDate: periodStart.AddMonths(1),
        OverrideReason: null,
        IsAbnormalConsumption: false,
        AbnormalConsumptionReason: null,
        Status: "Billed",
        ReviewedAtUtc: null,
        ReviewedBy: null,
        FinalizedAtUtc: null,
        FinalizedBy: null,
        BilledAtUtc: null,
        BilledBy: null,
        InvoiceId: null,
        CorrectsReadingId: null);

    [Fact]
    public async Task Loops_Through_Every_Page_And_Exports_Full_Dataset()
    {
        BuildingId buildingId = BuildingId.New();
        Meter meter = InstalledMeter(buildingId);
        Building building = Building.Create(TenantId, "Aisha Tower", "AISHA", null, DateTimeOffset.UtcNow);

        _meters.GetByIdAsync(meter.Id, Arg.Any<CancellationToken>()).Returns(meter);
        _buildings.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(building);

        List<ReadingDto> page1 = Enumerable.Range(0, 100)
            .Select(i => SampleReading(meter.Id.Value, new DateOnly(2026, 1, 1).AddMonths(i)))
            .ToList();
        List<ReadingDto> page2 = [SampleReading(meter.Id.Value, new DateOnly(2034, 5, 1))];

        _sender.Send(Arg.Is<GetReadingsQuery>(q => q.Page == 1), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ReadingDto>(page1, 1, 100, 101));
        _sender.Send(Arg.Is<GetReadingsQuery>(q => q.Page == 2), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ReadingDto>(page2, 2, 100, 101));

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "consumption-history.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportConsumptionHistoryReportQuery(meter.Id.Value, ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetReadingsQuery>(q => q.MeterId == meter.Id.Value && q.Page == 1 && q.PageSize == 100),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<GetReadingsQuery>(q => q.MeterId == meter.Id.Value && q.Page == 2 && q.PageSize == 100),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Electricity Consumption History" && d.Rows.Count == 101),
            ReportExportFormat.Csv,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthenticated_User_Throws_Forbidden()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ExportConsumptionHistoryReportQuery(Guid.NewGuid(), ReportExportFormat.Pdf), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Meter_Belonging_To_Another_Tenant_Throws_NotFound()
    {
        BuildingId buildingId = BuildingId.New();
        Meter otherTenantsMeter = Meter.Install(Guid.NewGuid(), buildingId, UtilityType.Gas, "MTR-999", DateTimeOffset.UtcNow);
        _meters.GetByIdAsync(otherTenantsMeter.Id, Arg.Any<CancellationToken>()).Returns(otherTenantsMeter);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ExportConsumptionHistoryReportQuery(otherTenantsMeter.Id.Value, ReportExportFormat.Pdf), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Unknown_Meter_Throws_NotFound()
    {
        _meters.GetByIdAsync(Arg.Any<MeterId>(), Arg.Any<CancellationToken>()).Returns((Meter?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ExportConsumptionHistoryReportQuery(Guid.NewGuid(), ReportExportFormat.Pdf), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
