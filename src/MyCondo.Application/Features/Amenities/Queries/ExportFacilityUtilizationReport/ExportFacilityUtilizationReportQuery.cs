using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Amenities.Queries.ExportFacilityUtilizationReport;

public sealed record ExportFacilityUtilizationReportQuery(
    Guid? FacilityId,
    DateOnly FromDate,
    DateOnly ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
