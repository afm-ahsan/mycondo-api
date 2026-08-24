using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseSummaryReport;

public sealed record ExportExpenseSummaryReportQuery(DateOnly FromDate, DateOnly ToDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>;
