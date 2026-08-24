using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Audit.DTOs;
using MyCondo.Application.Features.Finance.Audit.Queries.ExportFinanceAuditLog;
using MyCondo.Application.Features.Finance.Audit.Queries.GetFinanceAuditLog;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Audit.Queries.ExportFinanceAuditLog;

public class ExportFinanceAuditLogQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportFinanceAuditLogQueryHandler CreateHandler() => new(_sender, _exportService);

    private static List<FinanceAuditLogEntryDto> SampleEntries() =>
    [
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "Jane Doe", "Invoice.Void", "Invoice", "abc123", "Voided due to duplicate", null),
    ];

    [Fact]
    public async Task Delegates_To_GetFinanceAuditLogQuery_And_Exports_Mapped_Document()
    {
        List<FinanceAuditLogEntryDto> entries = SampleEntries();
        _sender.Send(Arg.Any<GetFinanceAuditLogQuery>(), Arg.Any<CancellationToken>()).Returns(entries);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "finance-audit-log.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFinanceAuditLogQuery(100, ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetFinanceAuditLogQuery>(q => q.Take == 100), Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Finance Audit Log"),
            ReportExportFormat.Csv,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Uses_Default_Export_Take_When_Caller_Does_Not_Specify_One()
    {
        _sender.Send(Arg.Any<GetFinanceAuditLogQuery>(), Arg.Any<CancellationToken>()).Returns(SampleEntries());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "finance-audit-log.csv"));

        await CreateHandler().Handle(new ExportFinanceAuditLogQuery(0, ReportExportFormat.Csv), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<GetFinanceAuditLogQuery>(q => q.Take == 500), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_Requested_Format_Through_To_Export_Service()
    {
        _sender.Send(Arg.Any<GetFinanceAuditLogQuery>(), Arg.Any<CancellationToken>()).Returns(SampleEntries());
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "application/pdf", "finance-audit-log.pdf"));

        await CreateHandler().Handle(new ExportFinanceAuditLogQuery(100, ReportExportFormat.Pdf), CancellationToken.None);

        await _exportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Pdf, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
