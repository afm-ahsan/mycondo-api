using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportServiceChargeCollectionReport;

public sealed record ExportServiceChargeCollectionReportQuery(DateOnly FromDate, DateOnly ToDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>, ILifecycleReadOperation;
