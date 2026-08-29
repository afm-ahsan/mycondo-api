using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportOutstandingDuesReport;

public sealed record ExportOutstandingDuesReportQuery(Guid? BuildingId, ReportExportFormat Format)
    : IRequest<ReportExportResult>, ILifecycleReadOperation;
