using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.ExportFinancialSummaryReport;
using MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.ExportFinancialSummaryReport;

public class ExportFinancialSummaryReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportFinancialSummaryReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static readonly DateOnly FromDate = new(2026, 8, 1);
    private static readonly DateOnly ToDate = new(2026, 8, 31);

    private static FinancialSummaryDto SampleReport() => new(
        FromDate,
        ToDate,
        new DateOnly(2026, 8, 23),
        10_000m,
        7_500m,
        2_500m,
        3,
        1,
        2);

    [Fact]
    public async Task Delegates_To_GetFinancialSummaryReportQuery_And_Exports_Mapped_Document()
    {
        FinancialSummaryDto report = SampleReport();
        _sender.Send(Arg.Any<GetFinancialSummaryReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(
            new MemoryStream(), "text/csv", "financial-summary-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(
                Arg.Any<ReportExportDocument>(),
                ReportExportFormat.Csv,
                "financial-summary-2026-08-01-to-2026-08-31",
                Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFinancialSummaryReportQuery(null, FromDate, ToDate, ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetFinancialSummaryReportQuery>(q => q.FromDate == FromDate && q.ToDate == ToDate && q.BuildingId == null),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Financial Summary"),
            ReportExportFormat.Csv,
            "financial-summary-2026-08-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_And_BuildingId_Through()
    {
        Guid buildingId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetFinancialSummaryReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _sender.Send(Arg.Any<GetBuildingByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BuildingDto(buildingId, "Lakeview Towers", "LKV", null, null));
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "financial-summary-2026-08-01-to-2026-08-31.pdf"));

        await CreateHandler().Handle(
            new ExportFinancialSummaryReportQuery(buildingId, FromDate, ToDate, ReportExportFormat.Pdf), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<GetFinancialSummaryReportQuery>(q => q.BuildingId == buildingId), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
