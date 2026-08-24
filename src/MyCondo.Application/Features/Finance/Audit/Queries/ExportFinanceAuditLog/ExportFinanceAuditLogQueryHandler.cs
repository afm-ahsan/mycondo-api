using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Audit.DTOs;
using MyCondo.Application.Features.Finance.Audit.Queries.GetFinanceAuditLog;

namespace MyCondo.Application.Features.Finance.Audit.Queries.ExportFinanceAuditLog;

/// <summary>Reuses <see cref="GetFinanceAuditLogQuery"/> for the actual data (tenant scoping, actor
/// name resolution) rather than duplicating that logic — this handler's only job is mapping the
/// resulting entries to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFinanceAuditLogQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFinanceAuditLogQuery, ReportExportResult>
{
    /// <summary>The GET route's on-screen default (100) exists to keep a scrollable list readable —
    /// it isn't a meaningful ceiling for an export. When the caller doesn't specify <c>take</c>,
    /// export the full recent window the repository allows instead (mirrors
    /// <c>GetFinanceAuditLogQueryHandler</c>'s own 500 clamp), so a compliance/record-keeping export
    /// isn't silently truncated to the UI's smaller page size.</summary>
    private const int DefaultExportTake = 500;

    public async ValueTask<ReportExportResult> Handle(ExportFinanceAuditLogQuery query, CancellationToken cancellationToken)
    {
        int take = query.Take <= 0 ? DefaultExportTake : query.Take;
        List<FinanceAuditLogEntryDto> entries = await sender.Send(new GetFinanceAuditLogQuery(take), cancellationToken);
        ReportExportDocument document = FinanceAuditLogExportMapper.ToExportDocument(entries);

        string fileName = $"finance-audit-log-{DateTime.UtcNow:yyyy-MM-dd}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
