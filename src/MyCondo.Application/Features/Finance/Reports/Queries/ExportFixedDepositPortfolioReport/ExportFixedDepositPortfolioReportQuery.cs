using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositPortfolioReport;

public sealed record ExportFixedDepositPortfolioReportQuery(DateOnly? AsOfDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>, ILifecycleReadOperation;
