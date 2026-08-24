using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Integrity.DTOs;
using MyCondo.Application.Features.Finance.Integrity.Queries.GetFinancialIntegrityDashboard;

namespace MyCondo.Application.Features.Finance.Integrity.Queries.ExportFinancialIntegrityDashboard;

/// <summary>Reuses <see cref="GetFinancialIntegrityDashboardQuery"/> for the actual checks (tenant
/// scoping, count computation) rather than duplicating that logic — this handler's only job is
/// mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it
/// to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFinancialIntegrityDashboardQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFinancialIntegrityDashboardQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(
        ExportFinancialIntegrityDashboardQuery query, CancellationToken cancellationToken)
    {
        FinancialIntegrityDashboardDto dashboard = await sender.Send(new GetFinancialIntegrityDashboardQuery(), cancellationToken);
        ReportExportDocument document = FinancialIntegrityDashboardExportMapper.ToExportDocument(dashboard);

        string fileName = $"financial-integrity-dashboard-{DateTime.UtcNow:yyyy-MM-dd}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
