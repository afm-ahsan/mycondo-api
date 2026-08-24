using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFlatFinancialStatement;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFlatFinancialStatement;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFlatFinancialStatement;

public class ExportFlatFinancialStatementQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();
    private readonly Guid _flatId = Guid.NewGuid();

    private ExportFlatFinancialStatementQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FinanceReportMetadataDto SampleMetadata() => FinanceReportMetadataDto.ForPeriod(
        new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Flat A-101", DateTimeOffset.UtcNow, null);

    private static FlatFinancialStatementLineDto Line(decimal runningBalance) => new(
        Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 5), "Service charge", "Invoice", Guid.NewGuid(), "Debit", 500m, runningBalance);

    [Fact]
    public async Task Single_Page_Result_Fetches_Once_And_Exports_All_Lines()
    {
        FlatFinancialStatementReportDto page1 = new(
            SampleMetadata(), _flatId, "A-101", 0m, 500m, 0m, 500m, [Line(500m)], 1, 100, 1);
        _sender.Send(Arg.Any<GetFlatFinancialStatementQuery>(), Arg.Any<CancellationToken>()).Returns(page1);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "flat-statement-A-101-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Is<ReportExportDocument>(d => d.Rows.Count == 1), ReportExportFormat.Csv,
                "flat-statement-A-101-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFlatFinancialStatementQuery(_flatId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(Arg.Any<GetFlatFinancialStatementQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Multi_Page_Result_Pages_Through_At_The_Lower_100_Cap_Until_Total_Reached()
    {
        FlatFinancialStatementLineDto[] page1Lines = Enumerable.Range(0, 100).Select(i => Line(500m * (i + 1))).ToArray();
        FlatFinancialStatementLineDto[] page2Lines = Enumerable.Range(0, 30).Select(i => Line(500m * (i + 101))).ToArray();

        FlatFinancialStatementReportDto page1 = new(
            SampleMetadata(), _flatId, "A-101", 0m, 65_000m, 0m, 50_000m, page1Lines, 1, 100, 130);
        FlatFinancialStatementReportDto page2 = new(
            SampleMetadata(), _flatId, "A-101", 0m, 65_000m, 0m, 65_000m, page2Lines, 2, 100, 130);

        _sender.Send(Arg.Is<GetFlatFinancialStatementQuery>(q => q.Page == 1 && q.PageSize == 100), Arg.Any<CancellationToken>()).Returns(page1);
        _sender.Send(Arg.Is<GetFlatFinancialStatementQuery>(q => q.Page == 2 && q.PageSize == 100), Arg.Any<CancellationToken>()).Returns(page2);

        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "flat-statement.csv"));

        await CreateHandler().Handle(
            new ExportFlatFinancialStatementQuery(_flatId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        await _sender.Received(2).Send(Arg.Any<GetFlatFinancialStatementQuery>(), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Rows.Count == 130 && d.Totals!.Any(t => t.Label == "Closing Balance" && t.Value == "65,000.00")),
            Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
