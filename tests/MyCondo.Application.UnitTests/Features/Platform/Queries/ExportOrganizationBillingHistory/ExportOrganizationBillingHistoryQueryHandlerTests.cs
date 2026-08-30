using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.ExportOrganizationBillingHistory;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationBillingHistory;
using MyCondo.Domain.Common;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.ExportOrganizationBillingHistory;

public class ExportOrganizationBillingHistoryQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _exportService = Substitute.For<IReportExportService>();

    private ExportOrganizationBillingHistoryQueryHandler CreateHandler() => new(_sender, _exportService);

    [Fact]
    public async Task Delegates_To_GetOrganizationBillingHistoryQuery_Requesting_The_Full_Window_Not_The_Screen_Page_Size()
    {
        Guid organizationId = Guid.NewGuid();
        List<OrganizationBillingHistoryEventDto> items =
            [new(new DateOnly(2026, 8, 1), "InvoiceIssued", Guid.NewGuid(), "SUBINV-1", "BDT", 5000m, "Issued", 5000m, null, null, null)];

        _sender.Send(Arg.Any<GetOrganizationBillingHistoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OrganizationBillingHistoryEventDto>(items, 1, 1000, 1));

        ReportExportResult expectedResult = new(new MemoryStream(), "text/csv", "organization-billing-history.csv");
        _exportService
            .ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportOrganizationBillingHistoryQuery(organizationId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), ReportExportFormat.Csv),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
        await _sender.Received(1).Send(
            Arg.Is<GetOrganizationBillingHistoryQuery>(q =>
                q.OrganizationId == organizationId && q.Page == 1 && q.PageSize >= 1000),
            Arg.Any<CancellationToken>());
    }
}
