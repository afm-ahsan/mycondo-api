using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialOverview;

public sealed record ExportFinancialOverviewQuery(
    DateOnly? AsOfDate,
    DateOnly? FromDate,
    DateOnly? ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, ILifecycleReadOperation;
