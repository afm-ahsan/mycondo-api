using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseByCategoryReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportExpenseByCategoryReport;

public class ExportExpenseByCategoryReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportExpenseByCategoryReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static ExpenseByCategoryReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForPeriod(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all expenses)", DateTimeOffset.UtcNow, null),
        [new ExpenseByCategoryLineDto(Guid.NewGuid(), "Utilities", 5_000m)],
        5_000m);

    [Fact]
    public async Task Delegates_To_GetExpenseByCategoryReportQuery_And_Exports_Mapped_Document()
    {
        ExpenseByCategoryReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetExpenseByCategoryReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(
            new MemoryStream(), "text/csv", "expense-by-category-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(
                Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv,
                "expense-by-category-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportExpenseByCategoryReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetExpenseByCategoryReportQuery>(q =>
                q.FromDate == new DateOnly(2026, 8, 1) && q.ToDate == new DateOnly(2026, 8, 31)),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Expense by Category"),
            ReportExportFormat.Csv,
            "expense-by-category-2026-08-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetExpenseByCategoryReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "expense-by-category-2026-08-01-to-2026-08-31.pdf"));

        await CreateHandler().Handle(
            new ExportExpenseByCategoryReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Pdf),
            CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
