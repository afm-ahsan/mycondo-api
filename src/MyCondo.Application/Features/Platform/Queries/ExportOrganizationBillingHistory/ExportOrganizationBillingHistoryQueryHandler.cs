using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationBillingHistory;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Platform.Queries.ExportOrganizationBillingHistory;

/// <summary>Reuses <see cref="GetOrganizationBillingHistoryQuery"/> for the actual merged-timeline data
/// rather than duplicating it — this handler's only job is requesting the full filtered timeline (not
/// the UI's smaller on-screen page) and mapping it to the format-agnostic
/// <see cref="ReportExportDocument"/> for the centralized <see cref="IReportExportService"/>. Mirrors
/// <c>ExportFinanceAuditLogQueryHandler</c>'s "export the full window, not the screen page size"
/// reasoning.</summary>
public sealed class ExportOrganizationBillingHistoryQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportOrganizationBillingHistoryQuery, ReportExportResult>
{
    private const int ExportPageSize = 1000;

    public async ValueTask<ReportExportResult> Handle(
        ExportOrganizationBillingHistoryQuery query, CancellationToken cancellationToken)
    {
        PagedResult<OrganizationBillingHistoryEventDto> page = await sender.Send(
            new GetOrganizationBillingHistoryQuery(query.OrganizationId, query.DateFrom, query.DateTo, Page: 1, PageSize: ExportPageSize),
            cancellationToken);
        ReportExportDocument document = OrganizationBillingHistoryExportMapper.ToExportDocument(
            page.Items, query.DateFrom, query.DateTo);

        string fileName = $"organization-billing-history-{query.OrganizationId}-{DateTime.UtcNow:yyyy-MM-dd}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
