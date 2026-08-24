using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Application.Features.Finance.Reports.Queries.ExportIncomeExpenseReport;
using MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.ExportIncomeExpenseReport;

public class ExportIncomeExpenseReportQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportIncomeExpenseReportQueryHandler CreateHandler() => new(_sender, _exportService);

    private static IncomeExpenseReportDto SampleReport() => new(
        FinanceReportMetadataDto.ForPeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), "Tenant (all accounts)", DateTimeOffset.UtcNow, null),
        [new IncomeExpenseLineDto(Guid.NewGuid(), "4010", "Service Charge Income", 20_000m)],
        20_000m,
        [new IncomeExpenseLineDto(Guid.NewGuid(), "5010", "Utilities Expense", 8_000m)],
        8_000m,
        12_000m);

    [Fact]
    public async Task Delegates_To_GetIncomeExpenseReportQuery_And_Exports_Mapped_Document()
    {
        IncomeExpenseReportDto report = SampleReport();
        _sender.Send(Arg.Any<GetIncomeExpenseReportQuery>(), Arg.Any<CancellationToken>()).Returns(report);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "income-expense-2026-08-01-to-2026-08-31.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, "income-expense-2026-08-01-to-2026-08-31", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportIncomeExpenseReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetIncomeExpenseReportQuery>(q => q.FromDate == new DateOnly(2026, 8, 1) && q.ToDate == new DateOnly(2026, 8, 31)),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Income & Expense"),
            ReportExportFormat.Csv,
            "income-expense-2026-08-01-to-2026-08-31",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetIncomeExpenseReportQuery>(), Arg.Any<CancellationToken>()).Returns(SampleReport());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "income-expense-2026-08-01-to-2026-08-31.pdf"));

        await CreateHandler().Handle(
            new ExportIncomeExpenseReportQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
