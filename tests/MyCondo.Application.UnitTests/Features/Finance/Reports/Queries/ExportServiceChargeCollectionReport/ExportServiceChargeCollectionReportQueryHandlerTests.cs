using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportServiceChargeCollectionReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportServiceChargeCollectionReport;

public class ExportServiceChargeCollectionReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportServiceChargeCollectionReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static ServiceChargeCollectionReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForPeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (Service Charge invoices)", DateTimeOffset.UtcNow, null),
        20_000m, 10, 18_000m, 500m);

    [Fact]
    public async Task Delegates_To_GetServiceChargeCollectionReportQuery_And_Exports_Mapped_Document()
    {
        ServiceChargeCollectionReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetServiceChargeCollectionReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "service-charge-collection-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "service-charge-collection-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportServiceChargeCollectionReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetServiceChargeCollectionReportQuery>(q => q.FromDate == new DateOnly(2026, 8, 1) && q.ToDate == new DateOnly(2026, 8, 31)),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Service Charge Collection"),
            ReportExportFormat.Csv,
            "service-charge-collection-2026-08-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetServiceChargeCollectionReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "service-charge-collection-2026-08-01-to-2026-08-31.pdf"));

        await CreateHandler().Handle(
            new ExportServiceChargeCollectionReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
