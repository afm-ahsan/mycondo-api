using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositPortfolioReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFixedDepositPortfolioReport;

public class ExportFixedDepositPortfolioReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportFixedDepositPortfolioReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FixedDepositPortfolioReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForAsOf(new DateOnly(2026, 8, 15), "Tenant (all Fixed Deposits)", DateTimeOffset.UtcNow, null),
        [new FixedDepositPortfolioLineDto(
            Guid.NewGuid(), "FD-001", "ABC Bank", "Main Branch", 100_000m, 6.5m,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), "Active", 3_000m, 2_000m, 1_000m)],
        100_000m,
        1_000m,
        1);

    [Fact]
    public async Task Delegates_To_GetFixedDepositPortfolioReportQuery_And_Exports_Mapped_Document()
    {
        FixedDepositPortfolioReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetFixedDepositPortfolioReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "fixed-deposit-portfolio-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "fixed-deposit-portfolio-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFixedDepositPortfolioReportQuery(new DateOnly(2026, 8, 15), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetFixedDepositPortfolioReportQuery>(q => q.AsOfDate == new DateOnly(2026, 8, 15)), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Fixed Deposit Portfolio"),
            ReportExportFormat.Csv,
            "fixed-deposit-portfolio-2026-08-15",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetFixedDepositPortfolioReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "fixed-deposit-portfolio-2026-08-15.pdf"));

        await CreateHandler().Handle(new ExportFixedDepositPortfolioReportQuery(null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
