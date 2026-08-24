using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportCashBankPositionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportCashBankPositionReport;

public class ExportCashBankPositionReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportCashBankPositionReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static CashBankPositionReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForAsOf(new DateOnly(2026, 8, 15), "Tenant (all financial accounts)", DateTimeOffset.UtcNow, null),
        [new CashBankAccountLineDto(Guid.NewGuid(), "Main Bank", "Bank", "ABC Bank", "Downtown", "12345", 15_000m)],
        15_000m);

    [Fact]
    public async Task Delegates_To_GetCashBankPositionReportQuery_And_Exports_Mapped_Document()
    {
        CashBankPositionReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetCashBankPositionReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "cash-bank-position-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "cash-bank-position-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportCashBankPositionReportQuery(new DateOnly(2026, 8, 15), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetCashBankPositionReportQuery>(q => q.AsOfDate == new DateOnly(2026, 8, 15)), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Cash & Bank Position"),
            ReportExportFormat.Csv,
            "cash-bank-position-2026-08-15",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetCashBankPositionReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "cash-bank-position-2026-08-15.pdf"));

        await CreateHandler().Handle(new ExportCashBankPositionReportQuery(null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
