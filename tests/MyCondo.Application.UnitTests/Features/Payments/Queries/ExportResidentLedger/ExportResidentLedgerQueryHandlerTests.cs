using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.ExportResidentLedger;
using MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;
using MyCondo.Domain.Common;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.ExportResidentLedger;

public class ExportResidentLedgerQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();
    private readonly Guid _flatId = Guid.NewGuid();

    private ExportResidentLedgerQueryHandler CreateHandler() => new(_sender, _exportService);

    private static LedgerEntryDto Entry(decimal amount, string direction = "Debit") => new(
        Guid.NewGuid(), Guid.NewGuid(), "Receivable", null, direction, amount,
        new DateOnly(2026, 8, 5), "Invoice charge", DateTimeOffset.UtcNow, "Invoice", Guid.NewGuid());

    [Fact]
    public async Task Single_Page_Result_Fetches_Once_And_Exports_All_Entries()
    {
        PagedResult<LedgerEntryDto> page1 = new([Entry(500m)], 1, 100, 1);
        _sender.Send(Arg.Any<GetLedgerEntriesForAccountQuery>(), Arg.Any<CancellationToken>()).Returns(page1);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "resident-ledger.csv");
        _exportService
            .ExportAsync(Arg.Is<ReportExportDocument>(d => d.Rows.Count == 1), ReportExportFormat.Csv,
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportResidentLedgerQuery(_flatId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null, ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(Arg.Any<GetLedgerEntriesForAccountQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Multi_Page_Result_Pages_Through_Until_Total_Reached_Without_Truncating()
    {
        LedgerEntryDto[] page1Entries = Enumerable.Range(0, 100).Select(_ => Entry(500m)).ToArray();
        LedgerEntryDto[] page2Entries = Enumerable.Range(0, 30).Select(_ => Entry(300m, "Credit")).ToArray();

        PagedResult<LedgerEntryDto> page1 = new(page1Entries, 1, 100, 130);
        PagedResult<LedgerEntryDto> page2 = new(page2Entries, 2, 100, 130);

        _sender.Send(Arg.Is<GetLedgerEntriesForAccountQuery>(q => q.Page == 1), Arg.Any<CancellationToken>()).Returns(page1);
        _sender.Send(Arg.Is<GetLedgerEntriesForAccountQuery>(q => q.Page == 2), Arg.Any<CancellationToken>()).Returns(page2);

        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "resident-ledger.csv"));

        await CreateHandler().Handle(
            new ExportResidentLedgerQuery(_flatId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null, ReportExportFormat.Csv),
            CancellationToken.None);

        await _sender.Received(2).Send(Arg.Any<GetLedgerEntriesForAccountQuery>(), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d =>
                d.Rows.Count == 130
                && d.Totals!.Any(t => t.Label == "Total Debit" && t.Value == "50,000.00")
                && d.Totals!.Any(t => t.Label == "Total Credit" && t.Value == "9,000.00")),
            Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_Result_Exports_Zero_Rows_Without_Looping_Forever()
    {
        PagedResult<LedgerEntryDto> emptyPage = new([], 1, 100, 0);
        _sender.Send(Arg.Any<GetLedgerEntriesForAccountQuery>(), Arg.Any<CancellationToken>()).Returns(emptyPage);

        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "resident-ledger.csv"));

        await CreateHandler().Handle(
            new ExportResidentLedgerQuery(_flatId, null, null, null, ReportExportFormat.Csv),
            CancellationToken.None);

        await _sender.Received(1).Send(Arg.Any<GetLedgerEntriesForAccountQuery>(), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Rows.Count == 0),
            Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
