using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialPosition;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportFinancialPosition;

public class ExportFinancialPositionQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportFinancialPositionQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FinancialPositionReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForAsOf(new DateOnly(2026, 8, 15), "Tenant (all accounts)", DateTimeOffset.UtcNow, null),
        new FinancialPositionSectionDto([new FinancialPositionLineDto(Guid.NewGuid(), "1010", "Cash", 10_000m)], 10_000m),
        new FinancialPositionSectionDto([], 0m),
        new FinancialPositionSectionDto([], 0m),
        2_000m,
        12_000m);

    [Fact]
    public async Task Delegates_To_GetFinancialPositionQuery_And_Exports_Mapped_Document()
    {
        FinancialPositionReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetFinancialPositionQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "financial-position-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "financial-position-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFinancialPositionQuery(new DateOnly(2026, 8, 15), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetFinancialPositionQuery>(q => q.AsOfDate == new DateOnly(2026, 8, 15)), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Financial Position"),
            ReportExportFormat.Csv,
            "financial-position-2026-08-15",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetFinancialPositionQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "financial-position-2026-08-15.pdf"));

        await CreateHandler().Handle(new ExportFinancialPositionQuery(null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
