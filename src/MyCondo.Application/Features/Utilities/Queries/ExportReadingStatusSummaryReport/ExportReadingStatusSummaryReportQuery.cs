using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Utilities.Queries.ExportReadingStatusSummaryReport;

public sealed record ExportReadingStatusSummaryReportQuery(
    Guid? BuildingId,
    string? UtilityType,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "utilities.reports";
}
