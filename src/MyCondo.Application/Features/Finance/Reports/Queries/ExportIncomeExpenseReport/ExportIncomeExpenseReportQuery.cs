using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportIncomeExpenseReport;

public sealed record ExportIncomeExpenseReportQuery(DateOnly FromDate, DateOnly ToDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>, ILifecycleReadOperation;
