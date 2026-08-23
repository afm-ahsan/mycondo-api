using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportTrialBalance;

public sealed record ExportTrialBalanceQuery(DateOnly? AsOfDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>;
