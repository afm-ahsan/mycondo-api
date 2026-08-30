using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Amenities.Queries.ExportBookingRevenueReport;

public sealed record ExportBookingRevenueReportQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "facilities.reports";
}
