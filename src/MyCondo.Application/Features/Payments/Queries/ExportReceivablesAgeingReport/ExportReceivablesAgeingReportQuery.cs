using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Payments.Queries.ExportReceivablesAgeingReport;

public sealed record ExportReceivablesAgeingReportQuery(
    Guid? BuildingId,
    DateOnly? AsOfDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
