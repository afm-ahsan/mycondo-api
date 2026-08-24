using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.ExportReceivablesAgeingReport;
using MyCondo.Application.Features.Payments.Queries.GetReceivablesAgeingReport;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.ExportReceivablesAgeingReport;

public class ExportReceivablesAgeingReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportReceivablesAgeingReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static ReceivablesAgeingReportDto SampleReport() => new(
        new DateOnly(2026, 8, 15),
        [new AgeingBucketDto("Current", null, 0, 2, 5_000m)],
        5_000m);

    [Fact]
    public async Task Delegates_To_GetReceivablesAgeingReportQuery_And_Exports_Mapped_Document()
    {
        ReceivablesAgeingReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetReceivablesAgeingReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "receivables-ageing-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "receivables-ageing-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportReceivablesAgeingReportQuery(null, new DateOnly(2026, 8, 15), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetReceivablesAgeingReportQuery>(q => q.AsOfDate == new DateOnly(2026, 8, 15)), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Receivables Ageing"),
            ReportExportFormat.Csv,
            "receivables-ageing-2026-08-15",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetReceivablesAgeingReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "receivables-ageing-2026-08-15.pdf"));

        await CreateHandler().Handle(new ExportReceivablesAgeingReportQuery(null, null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolves_Building_Name_When_BuildingId_Provided()
    {
        Guid buildingId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetReceivablesAgeingReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _sender.Send(Arg.Any<GetBuildingByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BuildingDto(buildingId, "Lakeview Towers", "LKV", null, null));
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "receivables-ageing-2026-08-15.csv"));

        await CreateHandler().Handle(
            new ExportReceivablesAgeingReportQuery(buildingId, new DateOnly(2026, 8, 15), ReportExportFormat.Csv), CancellationToken.None);

        await _sender.Received(1).Send(Arg.Is<GetBuildingByIdQuery>(q => q.BuildingId == buildingId), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.MetadataLines.Any(m => m.Label == "Scope" && m.Value == "Lakeview Towers")),
            Arg.Any<ReportExportFormat>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
