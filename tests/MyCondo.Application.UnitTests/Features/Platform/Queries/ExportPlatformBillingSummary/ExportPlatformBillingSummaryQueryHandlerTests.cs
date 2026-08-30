using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.ExportPlatformBillingSummary;
using MyCondo.Application.Features.Platform.Queries.GetPlatformBillingSummary;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.ExportPlatformBillingSummary;

public class ExportPlatformBillingSummaryQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportPlatformBillingSummaryQueryHandler CreateHandler() => new(_sender, _exportService);

    [Fact]
    public async Task Delegates_To_GetPlatformBillingSummaryQuery_And_Exports_Mapped_Document()
    {
        List<PlatformBillingSummaryCurrencyDto> summary = [new("BDT", 8000m, 2, 2000m, 1, 6000m, 2, 5000m, 1, 15)];
        _sender.Send(Arg.Any<GetPlatformBillingSummaryQuery>(), Arg.Any<CancellationToken>()).Returns(summary);

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "platform-billing-summary.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        Guid organizationId = Guid.NewGuid();
        ReportExportResult result = await CreateHandler().Handle(
            new ExportPlatformBillingSummaryQuery(organizationId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetPlatformBillingSummaryQuery>(q =>
                q.OrganizationId == organizationId && q.DateFrom == new DateOnly(2026, 8, 1) && q.DateTo == new DateOnly(2026, 8, 31)),
            Arg.Any<CancellationToken>());
        await _exportService.Received(1).ExportAsync(
            Arg.Is<ReportExportDocument>(d => d.Title == "Platform Subscription Billing Summary"),
            ReportExportFormat.Csv,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
