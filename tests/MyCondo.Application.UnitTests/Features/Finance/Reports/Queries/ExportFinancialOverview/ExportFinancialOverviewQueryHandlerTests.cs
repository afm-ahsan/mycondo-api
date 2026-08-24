using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialOverview;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFinancialOverview;

public class ExportFinancialOverviewQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportFinancialOverviewQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FinancialOverviewReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForAsOf(new DateOnly(2026, 8, 15), "Tenant (all accounts)", DateTimeOffset.UtcNow, null),
        50_000m, 30_000m, 20_000m, 10_000m,
        new CashPositionDto(1_000m, 5_000m, 500m, 6_500m),
        25_000m,
        new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 15),
        new CollectionPerformanceDto(12_000m, 10_000m),
        [new ExpenseCompositionLineDto(Guid.NewGuid(), "Utilities", 30_000m, 100m)]);

    [Fact]
    public async Task Delegates_To_GetFinancialOverviewQuery_And_Exports_Mapped_Document()
    {
        FinancialOverviewReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetFinancialOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "overview-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "overview-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFinancialOverviewQuery(new DateOnly(2026, 8, 15), null, null, ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetFinancialOverviewQuery>(q => q.AsOfDate == new DateOnly(2026, 8, 15)), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Financial Overview"),
            ReportExportFormat.Csv,
            "overview-2026-08-15",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetFinancialOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "overview-2026-08-15.pdf"));

        await CreateHandler().Handle(new ExportFinancialOverviewQuery(null, null, null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
