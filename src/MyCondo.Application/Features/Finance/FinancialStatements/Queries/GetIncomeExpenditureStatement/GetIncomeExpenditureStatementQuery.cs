using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;

/// <summary>One account's signed contribution to an <see cref="IncomeExpenditureStatementGroupDto"/>
/// line — the same drill-down unit Task 5/6 will navigate from.</summary>
public sealed record IncomeExpenditureAccountLineDto(Guid ChartOfAccountId, string Code, string Name, decimal Amount);

/// <summary>One <see cref="FinancialStatementGroup"/> bucket rendered as a statement line, plus the
/// accounts that roll up into it.</summary>
public sealed record IncomeExpenditureStatementGroupDto(
    FinancialStatementGroup Group, decimal Amount, IReadOnlyList<IncomeExpenditureAccountLineDto> Accounts);

/// <summary>One statement section (Income / Expenditure) — its GL-classified groups plus the section
/// total.</summary>
public sealed record IncomeExpenditureStatementSectionDto(
    IReadOnlyList<IncomeExpenditureStatementGroupDto> Groups, decimal Total);

/// <summary>A non-zero Income or Expense account with no explicit <c>ChartOfAccount.StatementGroup</c>,
/// surfaced so the statement never silently drops it (Phase 2A plan §8/§15). Scoped to Income/Expense
/// only — an unmapped Asset/Liability account is not this statement's concern.</summary>
public sealed record UnmappedIncomeExpenditureAccountWarningDto(
    Guid ChartOfAccountId, string Code, string Name, AccountCategory Category,
    FinancialStatementGroup EffectiveStatementGroup, decimal Amount);

/// <summary>The Income &amp; Expenditure Statement — CondoBD Finance Phase 2A Task 4. Every figure is
/// derived from the Task 2 period GL snapshot for [<see cref="GetIncomeExpenditureStatementQuery.StartDate"/>,
/// <see cref="GetIncomeExpenditureStatementQuery.EndDate"/>]. <see cref="SurplusDeficit"/> is
/// <see cref="TotalIncome"/> minus <see cref="TotalExpenditure"/> for this period only — unlike
/// <c>StatementOfFinancialPositionDto.CumulativeSurplusDeficit</c>, which is cumulative since
/// inception, this figure is naturally period-scoped because it is derived from a period snapshot, not
/// an as-of-date one. Expenditure granularity is limited to what the General Ledger can truthfully
/// distinguish today — see the handler doc comment.</summary>
public sealed record IncomeExpenditureStatementDto(
    FinanceReportMetadataDto Metadata,
    Guid? FundId,
    IncomeExpenditureStatementSectionDto Income,
    IncomeExpenditureStatementSectionDto Expenditure,
    decimal TotalIncome,
    decimal TotalExpenditure,
    decimal SurplusDeficit,
    IReadOnlyList<UnmappedIncomeExpenditureAccountWarningDto> UnmappedAccountWarnings);

/// <summary>Period Income &amp; Expenditure Statement. <see cref="StartDate"/>/<see cref="EndDate"/> are
/// both inclusive; <see cref="FundId"/> omitted means "All Funds".</summary>
public sealed record GetIncomeExpenditureStatementQuery(DateOnly StartDate, DateOnly EndDate, Guid? FundId)
    : IRequest<IncomeExpenditureStatementDto>;
