using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;

/// <summary>One account's signed contribution to a <see cref="StatementOfFinancialPositionGroupDto"/>
/// line — the same drill-down unit Task 5/6 will navigate from.</summary>
public sealed record StatementOfFinancialPositionAccountLineDto(Guid ChartOfAccountId, string Code, string Name, decimal Amount);

/// <summary>One <see cref="FinancialStatementGroup"/> bucket rendered as a statement line, plus the
/// accounts that roll up into it.</summary>
public sealed record StatementOfFinancialPositionGroupDto(
    FinancialStatementGroup Group, decimal Amount, IReadOnlyList<StatementOfFinancialPositionAccountLineDto> Accounts);

/// <summary>One statement section (Assets / Liabilities / Funds) — its GL-classified groups plus the
/// section subtotal.</summary>
public sealed record StatementOfFinancialPositionSectionDto(
    IReadOnlyList<StatementOfFinancialPositionGroupDto> Groups, decimal Total);

/// <summary>A non-zero account with no explicit <c>ChartOfAccount.StatementGroup</c>, surfaced so the
/// statement never silently drops it (Phase 2A plan §8/§15). Includes Income/Expense accounts too, since
/// those feed <see cref="StatementOfFinancialPositionDto.CumulativeSurplusDeficit"/>.</summary>
public sealed record UnmappedStatementAccountWarningDto(
    Guid ChartOfAccountId, string Code, string Name, AccountCategory Category,
    FinancialStatementGroup EffectiveStatementGroup, decimal Amount);

/// <summary>The Statement of Financial Position (Balance Sheet) — CondoBD Finance Phase 2A Task 3.
/// Every figure is derived from the Task 2 as-of-date GL snapshot except
/// <see cref="CumulativeSurplusDeficit"/>, which is computed from that same snapshot's Income/Expense
/// category totals rather than from a <see cref="FinancialStatementGroup"/> (there is no such group —
/// see the handler for why). <see cref="Funds"/> contains only the GL Equity-category groups
/// (Fund Balance, Accumulated Surplus); a renderer should present
/// <see cref="CumulativeSurplusDeficit"/> as that section's third line, per the Phase 2A plan §9
/// target layout — see the handler doc comment for why this is a cumulative-since-inception figure,
/// not a true current-period one, and what changes once formal period closing exists.</summary>
public sealed record StatementOfFinancialPositionDto(
    FinanceReportMetadataDto Metadata,
    Guid? FundId,
    StatementOfFinancialPositionSectionDto Assets,
    StatementOfFinancialPositionSectionDto Liabilities,
    StatementOfFinancialPositionSectionDto Funds,
    decimal CumulativeSurplusDeficit,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalFunds,
    decimal TotalLiabilitiesAndFunds,
    decimal Difference,
    bool IsBalanced,
    IReadOnlyList<UnmappedStatementAccountWarningDto> UnmappedAccountWarnings);

/// <summary>As-of-date Balance Sheet. <see cref="AsOfDate"/> defaults to today (tenant clock) when
/// omitted; <see cref="FundId"/> omitted means "All Funds".</summary>
public sealed record GetStatementOfFinancialPositionQuery(DateOnly? AsOfDate, Guid? FundId)
    : IRequest<StatementOfFinancialPositionDto>;
