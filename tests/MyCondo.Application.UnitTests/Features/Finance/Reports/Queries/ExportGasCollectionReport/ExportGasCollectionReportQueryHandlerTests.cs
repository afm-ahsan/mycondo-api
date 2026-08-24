using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportGasCollectionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportGasCollectionReport;

public class ExportGasCollectionReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportGasCollectionReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static GasCollectionReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForPeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (Gas recovery invoices)", DateTimeOffset.UtcNow, null),
        6_000m, 5, 5_500m, 0m);

    [Fact]
    public async Task Delegates_To_GetGasCollectionReportQuery_And_Exports_Mapped_Document()
    {
        GasCollectionReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetGasCollectionReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "gas-collection-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "gas-collection-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportGasCollectionReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetGasCollectionReportQuery>(q => q.FromDate == new DateOnly(2026, 8, 1) && q.ToDate == new DateOnly(2026, 8, 31)),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Gas Collection"),
            ReportExportFormat.Csv,
            "gas-collection-2026-08-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetGasCollectionReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "gas-collection-2026-08-01-to-2026-08-31.pdf"));

        await CreateHandler().Handle(
            new ExportGasCollectionReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
