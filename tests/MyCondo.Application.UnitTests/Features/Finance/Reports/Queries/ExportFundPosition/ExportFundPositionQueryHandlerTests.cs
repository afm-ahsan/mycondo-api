using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFundPosition;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFundPosition;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFundPosition;

public class ExportFundPositionQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportFundPositionQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FundPositionReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForAsOf(new DateOnly(2026, 8, 15), "Tenant (all funds)", DateTimeOffset.UtcNow, null),
        [new FundPositionLineDto(Guid.NewGuid(), "SNK", "Sinking Fund", 25_000m)],
        25_000m);

    [Fact]
    public async Task Delegates_To_GetFundPositionQuery_And_Exports_Mapped_Document()
    {
        FundPositionReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetFundPositionQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "fund-position-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "fund-position-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFundPositionQuery(new DateOnly(2026, 8, 15), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetFundPositionQuery>(q => q.AsOfDate == new DateOnly(2026, 8, 15)), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Fund Position"),
            ReportExportFormat.Csv,
            "fund-position-2026-08-15",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetFundPositionQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "fund-position-2026-08-15.pdf"));

        await CreateHandler().Handle(new ExportFundPositionQuery(null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
