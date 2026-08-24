using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Operations.Queries.ExportGeneratorMaintenanceDueReport;

public sealed record ExportGeneratorMaintenanceDueReportQuery(ReportExportFormat Format) : IRequest<ReportExportResult>;
