using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.ExportConsumptionSummaryReport;
using MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.ExportConsumptionSummaryReport;

public class ExportConsumptionSummaryReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportConsumptionSummaryReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static IReadOnlyList<ConsumptionSummaryLineDto> SampleReport() =>
        [new ConsumptionSummaryLineDto("Electricity", 1_250m, 10)];

    [Fact]
    public async Task Delegates_To_GetConsumptionSummaryReportQuery_And_Exports_Mapped_Document()
    {
        DateOnly fromDate = new(2026, 8, 1);
        DateOnly toDate = new(2026, 8, 31);
        Guid buildingId = Guid.NewGuid();

        _sender.Send(Arg.Any<GetConsumptionSummaryReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "consumption-summary-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv,
                "consumption-summary-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportConsumptionSummaryReportQuery(buildingId, "Electricity", fromDate, toDate, ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetConsumptionSummaryReportQuery>(q =>
                q.BuildingId == buildingId && q.UtilityType == "Electricity" && q.FromDate == fromDate && q.ToDate == toDate),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Consumption Summary"),
            ReportExportFormat.Csv,
            "consumption-summary-2026-08-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetConsumptionSummaryReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "consumption-summary.pdf"));

        await CreateHandler().Handle(
            new ExportConsumptionSummaryReportQuery(null, null, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Pdf),
            CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
