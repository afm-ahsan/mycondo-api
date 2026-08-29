using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportCashBankPositionReport;

public sealed record ExportCashBankPositionReportQuery(DateOnly? AsOfDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>, ILifecycleReadOperation;
