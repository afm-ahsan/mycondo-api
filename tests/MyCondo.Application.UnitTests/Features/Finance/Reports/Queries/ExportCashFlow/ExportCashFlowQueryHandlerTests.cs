using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportCashFlow;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportCashFlow;

public class ExportCashFlowQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportCashFlowQueryHandler CreateHandler() => new(_sender, _exportService);

    private static CashFlowReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForPeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all cash/bank accounts)", DateTimeOffset.UtcNow, null),
        10_000m, 5_000m, 2_000m, 3_000m, 1_000m, 500m, 500m, 3_500m, 13_500m);

    [Fact]
    public async Task Delegates_To_GetCashFlowQuery_And_Exports_Mapped_Document()
    {
        CashFlowReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetCashFlowQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "cash-flow-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "cash-flow-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportCashFlowQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetCashFlowQuery>(q => q.FromDate == new DateOnly(2026, 8, 1) && q.ToDate == new DateOnly(2026, 8, 31)),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Cash Flow"),
            ReportExportFormat.Csv,
            "cash-flow-2026-08-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetCashFlowQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "cash-flow-2026-08-01-to-2026-08-31.pdf"));

        await CreateHandler().Handle(
            new ExportCashFlowQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
