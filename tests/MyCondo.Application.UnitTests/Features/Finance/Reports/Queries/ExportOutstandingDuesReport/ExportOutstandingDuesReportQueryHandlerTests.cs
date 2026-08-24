using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportOutstandingDuesReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportOutstandingDuesReport;

public class ExportOutstandingDuesReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportOutstandingDuesReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static OutstandingDuesReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForAsOf(new DateOnly(2026, 8, 15), "Tenant (all buildings)", DateTimeOffset.UtcNow, null),
        [new OutstandingDuesLineDto(Guid.NewGuid(), "A-101", Guid.NewGuid(), 3_500m, 2)],
        3_500m);

    [Fact]
    public async Task Delegates_To_GetOutstandingDuesReportQuery_And_Exports_Mapped_Document()
    {
        OutstandingDuesReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetOutstandingDuesReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "outstanding-dues-2026-08-15.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "outstanding-dues-2026-08-15", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        Guid buildingId = Guid.NewGuid();
        ReportExportResult result = await CreateHandler().Handle(
            new ExportOutstandingDuesReportQuery(buildingId, ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetOutstandingDuesReportQuery>(q => q.BuildingId == buildingId), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Outstanding Dues"),
            ReportExportFormat.Csv,
            "outstanding-dues-2026-08-15",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetOutstandingDuesReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "outstanding-dues-2026-08-15.pdf"));

        await CreateHandler().Handle(new ExportOutstandingDuesReportQuery(null, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
