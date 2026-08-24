using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Payments.Queries.ExportFinancialSummaryReport;

public sealed record ExportFinancialSummaryReportQuery(
    Guid? BuildingId,
    DateOnly FromDate,
    DateOnly ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
