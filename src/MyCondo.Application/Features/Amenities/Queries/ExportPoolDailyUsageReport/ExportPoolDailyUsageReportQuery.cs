using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Amenities.Queries.ExportPoolDailyUsageReport;

public sealed record ExportPoolDailyUsageReportQuery(
    Guid FacilityId,
    DateOnly Date,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "facilities.reports";
}
