using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseTrendReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportExpenseTrendReport;

public class ExportExpenseTrendReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportExpenseTrendReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static ExpenseTrendReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31), "Tenant (all expenses)", DateTimeOffset.UtcNow, null),
        [new ExpenseTrendMonthDto(2026, 8, 5_000m)],
        5_000m);

    [Fact]
    public async Task Delegates_To_GetExpenseTrendReportQuery_And_Exports_Mapped_Document()
    {
        ExpenseTrendReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetExpenseTrendReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(
            new MemoryStream(), "text/csv", "expense-trend-2026-06-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(
                Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv,
                "expense-trend-2026-06-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportExpenseTrendReportQuery(new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetExpenseTrendReportQuery>(q =>
                q.FromDate == new DateOnly(2026, 6, 1) && q.ToDate == new DateOnly(2026, 8, 31)),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Expense Trend"),
            ReportExportFormat.Csv,
            "expense-trend-2026-06-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetExpenseTrendReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "expense-trend-2026-06-01-to-2026-08-31.pdf"));

        await CreateHandler().Handle(
            new ExportExpenseTrendReportQuery(new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Pdf),
            CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
