using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.ExportReadingStatusSummaryReport;
using MyCondo.Application.Features.Utilities.Queries.GetReadingStatusSummaryReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.ExportReadingStatusSummaryReport;

public class ExportReadingStatusSummaryReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportReadingStatusSummaryReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static IReadOnlyList<ReadingStatusSummaryLineDto> SampleReport() =>
        [new ReadingStatusSummaryLineDto("Electricity", "Billed", 5)];

    [Fact]
    public async Task Delegates_To_GetReadingStatusSummaryReportQuery_And_Exports_Mapped_Document()
    {
        Guid buildingId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetReadingStatusSummaryReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());

        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "reading-status-summary.csv"));

        await CreateHandler().Handle(
            new ExportReadingStatusSummaryReportQuery(buildingId, "Electricity", ReportExportFormat.Csv), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<GetReadingStatusSummaryReportQuery>(q => q.BuildingId == buildingId && q.UtilityType == "Electricity"),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Reading Status Summary"),
            ReportExportFormat.Csv,
            Arg.Is<string>(f => f.StartsWith("reading-status-summary-")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetReadingStatusSummaryReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "reading-status-summary.pdf"));

        await CreateHandler().Handle(
            new ExportReadingStatusSummaryReportQuery(null, null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
