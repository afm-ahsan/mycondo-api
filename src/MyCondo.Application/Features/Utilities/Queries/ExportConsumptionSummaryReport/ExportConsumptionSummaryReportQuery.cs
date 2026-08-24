using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Utilities.Queries.ExportConsumptionSummaryReport;

public sealed record ExportConsumptionSummaryReportQuery(
    Guid? BuildingId,
    string? UtilityType,
    DateOnly FromDate,
    DateOnly ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
