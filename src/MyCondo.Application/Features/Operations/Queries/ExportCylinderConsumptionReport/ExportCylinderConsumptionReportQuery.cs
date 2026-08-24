using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Operations.Queries.ExportCylinderConsumptionReport;

public sealed record ExportCylinderConsumptionReportQuery(
    string? CylinderType,
    DateOnly FromDate,
    DateOnly ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
