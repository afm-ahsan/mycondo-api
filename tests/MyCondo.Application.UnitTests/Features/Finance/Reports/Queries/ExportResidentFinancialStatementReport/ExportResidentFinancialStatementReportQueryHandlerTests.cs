using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportResidentFinancialStatementReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportResidentFinancialStatementReport;

public class ExportResidentFinancialStatementReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();
    private readonly Guid _flatId = Guid.NewGuid();

    private ExportResidentFinancialStatementReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FinanceReportMetadataDto SampleMetadata() => FinanceReportMetadataDto.ForPeriod(
        new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Flat A-101", DateTimeOffset.UtcNow, null);

    private static ResidentStatementLineDto Line(decimal runningBalance) => new(
        Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 5), "Service charge", "Invoice", Guid.NewGuid(), "Debit", 500m, runningBalance);

    [Fact]
    public async Task Single_Page_Result_Fetches_Once_And_Exports_All_Lines()
    {
        ResidentFinancialStatementReportDto page1 = new(
            SampleMetadata(), _flatId, "A-101", 0m, [Line(500m)], 500m, 1, 200, 1);
        _sender.Send(Arg.Any<GetResidentFinancialStatementReportQuery>(), Arg.Any<CancellationToken>()).Returns(page1);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "resident-statement-A-101-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Is<ReportExportDocument>(d => d.Rows.Count == 1), ReportExportFormat.Csv,
                "resident-statement-A-101-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportResidentFinancialStatementReportQuery(_flatId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(Arg.Any<GetResidentFinancialStatementReportQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Multi_Page_Result_Pages_Through_Until_Total_Reached_Without_Truncating()
    {
        ResidentStatementLineDto[] page1Lines = Enumerable.Range(0, 200).Select(i => Line(500m * (i + 1))).ToArray();
        ResidentStatementLineDto[] page2Lines = Enumerable.Range(0, 50).Select(i => Line(500m * (i + 201))).ToArray();

        ResidentFinancialStatementReportDto page1 = new(SampleMetadata(), _flatId, "A-101", 0m, page1Lines, 100_000m, 1, 200, 250);
        ResidentFinancialStatementReportDto page2 = new(SampleMetadata(), _flatId, "A-101", 0m, page2Lines, 125_000m, 2, 200, 250);

        _sender.Send(Arg.Is<GetResidentFinancialStatementReportQuery>(q => q.Page == 1), Arg.Any<CancellationToken>()).Returns(page1);
        _sender.Send(Arg.Is<GetResidentFinancialStatementReportQuery>(q => q.Page == 2), Arg.Any<CancellationToken>()).Returns(page2);

        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "resident-statement.csv"));

        await CreateHandler().Handle(
            new ExportResidentFinancialStatementReportQuery(_flatId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        await _sender.Received(2).Send(Arg.Any<GetResidentFinancialStatementReportQuery>(), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Rows.Count == 250 && d.Totals!.Any(t => t.Label == "Closing Balance" && t.Value == "125,000.00")),
            Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
