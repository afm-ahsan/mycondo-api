using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;

public sealed record CashFlowReportDto(
    FinanceReportMetadataDto Metadata,
    decimal OpeningCashBalance,
    decimal OperatingInflow,
    decimal OperatingOutflow,
    decimal NetOperating,
    decimal InvestingInflow,
    decimal InvestingOutflow,
    decimal NetInvesting,
    decimal NetChangeInCash,
    decimal ClosingCashBalance);

public sealed record GetCashFlowQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<CashFlowReportDto>;
