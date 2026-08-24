using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportGeneralLedger;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportGeneralLedger;

public class ExportGeneralLedgerQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportGeneralLedgerQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FinanceReportMetadataDto SampleMetadata() => FinanceReportMetadataDto.ForPeriod(
        new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all accounts)", DateTimeOffset.UtcNow, null);

    private static GeneralLedgerLineDto Line() => new(
        Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 5), "Service charge posting", "Invoice", Guid.NewGuid(),
        Guid.NewGuid(), "4010", "Service Charge Income", null, null, "Credit", 500m);

    [Fact]
    public async Task Single_Page_Result_Fetches_Once_And_Exports_All_Lines()
    {
        GeneralLedgerReportDto page1 = new(SampleMetadata(), [Line(), Line()], 1, 200, 2);
        _sender.Send(Arg.Any<GetGeneralLedgerQuery>(), Arg.Any<CancellationToken>()).Returns(page1);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "general-ledger-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Is<ReportExportDocument>(d => d.Rows.Count == 2), ReportExportFormat.Csv,
                "general-ledger-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportGeneralLedgerQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null, null, null, ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(Arg.Any<GetGeneralLedgerQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Multi_Page_Result_Pages_Through_Until_Total_Reached_Without_Truncating()
    {
        GeneralLedgerLineDto[] page1Lines = Enumerable.Range(0, 200).Select(_ => Line()).ToArray();
        GeneralLedgerLineDto[] page2Lines = Enumerable.Range(0, 50).Select(_ => Line()).ToArray();

        GeneralLedgerReportDto page1 = new(SampleMetadata(), page1Lines, 1, 200, 250);
        GeneralLedgerReportDto page2 = new(SampleMetadata(), page2Lines, 2, 200, 250);

        _sender.Send(Arg.Is<GetGeneralLedgerQuery>(q => q.Page == 1), Arg.Any<CancellationToken>()).Returns(page1);
        _sender.Send(Arg.Is<GetGeneralLedgerQuery>(q => q.Page == 2), Arg.Any<CancellationToken>()).Returns(page2);

        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "general-ledger.csv"));

        await CreateHandler().Handle(
            new ExportGeneralLedgerQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null, null, null, ReportExportFormat.Csv),
            CancellationToken.None);

        await _sender.Received(2).Send(Arg.Any<GetGeneralLedgerQuery>(), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Rows.Count == 250),
            Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
