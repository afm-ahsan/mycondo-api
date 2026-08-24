using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Integrity.DTOs;
using MyCondo.Application.Features.Finance.Integrity.Queries.ExportFinancialIntegrityDashboard;
using MyCondo.Application.Features.Finance.Integrity.Queries.GetFinancialIntegrityDashboard;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Integrity.Queries.ExportFinancialIntegrityDashboard;

public class ExportFinancialIntegrityDashboardQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportFinancialIntegrityDashboardQueryHandler CreateHandler() => new(_sender, _exportService);

    private static FinancialIntegrityDashboardDto SampleDashboard() => new(0, 0, 0, 2, 1, true);

    [Fact]
    public async Task Delegates_To_GetFinancialIntegrityDashboardQuery_And_Exports_Mapped_Document()
    {
        FinancialIntegrityDashboardDto dashboard = SampleDashboard();
        _sender.Send(Arg.Any<GetFinancialIntegrityDashboardQuery>(), Arg.Any<CancellationToken>()).Returns(dashboard);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "financial-integrity-dashboard.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFinancialIntegrityDashboardQuery(ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(Arg.Any<GetFinancialIntegrityDashboardQuery>(), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Financial Integrity Dashboard"),
            ReportExportFormat.Csv,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetFinancialIntegrityDashboardQuery>(), Arg.Any<CancellationToken>()).Returns(SampleDashboard());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "financial-integrity-dashboard.pdf"));

        await CreateHandler().Handle(new ExportFinancialIntegrityDashboardQuery(ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
