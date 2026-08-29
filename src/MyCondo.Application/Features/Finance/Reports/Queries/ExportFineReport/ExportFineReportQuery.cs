using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFineReport;

public sealed record ExportFineReportQuery(DateOnly FromDate, DateOnly ToDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>, ILifecycleReadOperation;
