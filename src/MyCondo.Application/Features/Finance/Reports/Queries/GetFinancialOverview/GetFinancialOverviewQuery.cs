using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;

/// <summary>Cash/Bank holdings broken down by <c>FinancialAccountType</c> (Template 4) — precise labels
/// only, per Template 5's requirement, never an ambiguous merged "Total In-Hand".</summary>
public sealed record CashPositionDto(
    decimal CashInHand,
    decimal BankBalance,
    decimal MobileFinancialServiceBalance,
    decimal AvailableLiquidFunds);

/// <summary>Billed and Collected for the report's collection window — permanently distinct figures,
/// never merged into one "revenue" number (Template 5's core rule).</summary>
public sealed record CollectionPerformanceDto(decimal Billed, decimal Collected);

public sealed record ExpenseCompositionLineDto(Guid? ExpenseCategoryId, string CategoryName, decimal Amount, decimal PercentageOfTotal);

/// <summary>Composite dashboard figure. <see cref="IncomeTotal"/>/<see cref="ExpenseTotal"/>/
/// <see cref="SurplusDeficit"/> are cumulative-to-<see cref="Metadata"/>'s AsOfDate (same "as of" scope
/// as the Financial Position report's RetainedSurplusDeficit) — Surplus/Deficit is always Income minus
/// Expense, never Cash minus Expense. <see cref="Receivables"/> and <see cref="CashPosition"/> are also
/// as-of-date ledger balances. <see cref="CollectionPerformance"/> and <see cref="ExpenseComposition"/>
/// are instead scoped to [<see cref="CollectionPeriodFromDate"/>, <see cref="CollectionPeriodToDate"/>]
/// — a distinct period-flow window from the cumulative figures above, not a second truth for the same
/// question (they answer "what happened this period," not "what is the balance as of today").</summary>
public sealed record FinancialOverviewReportDto(
    FinanceReportMetadataDto Metadata,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal SurplusDeficit,
    decimal Receivables,
    CashPositionDto CashPosition,
    decimal FixedDepositPrincipal,
    DateOnly CollectionPeriodFromDate,
    DateOnly CollectionPeriodToDate,
    CollectionPerformanceDto CollectionPerformance,
    IReadOnlyList<ExpenseCompositionLineDto> ExpenseComposition);

/// <summary>AsOfDate drives every cumulative balance (Income/Expense/Surplus/Receivables/Cash/FD
/// principal). FromDate/ToDate independently scope the period-flow figures (collection performance,
/// expense composition) — both default to the calendar month containing AsOfDate when omitted.</summary>
public sealed record GetFinancialOverviewQuery(DateOnly? AsOfDate, DateOnly? FromDate, DateOnly? ToDate) : IRequest<FinancialOverviewReportDto>;
