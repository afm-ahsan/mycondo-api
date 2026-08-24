using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportGasCollectionReport;

public sealed record ExportGasCollectionReportQuery(DateOnly FromDate, DateOnly ToDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>;
