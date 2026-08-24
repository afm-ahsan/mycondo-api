using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialStatements.Services;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;

/// <summary>Composes Task 2's as-of-date GL snapshot into a Balance Sheet — no second aggregation path
/// over invoices, receivables, payments, expenses, FDs, or bank accounts.
///
/// This system has no period-end closing/retained-earnings posting step — the same finding the
/// pre-Phase-2A <c>GetFinancialPositionQueryHandler</c> documented. So
/// <see cref="StatementOfFinancialPositionDto.CumulativeSurplusDeficit"/> is cumulative Income minus
/// cumulative Expense through <c>AsOfDate</c> — every Income/Expense posting since inception, not just
/// the current reporting period — computed from the same snapshot's <see cref="AccountCategory.Income"/>/
/// <see cref="AccountCategory.Expense"/> totals. This is the correct interim reporting treatment because
/// formal period/year-end closing is not implemented yet (Implementation Plan §18 explicitly defers it);
/// once it exists, Financial Position should distinguish prior accumulated surplus (carried forward by a
/// closing entry) from a true current-period surplus/deficit, and this field's name/shape should change
/// accordingly rather than being reinterpreted in place.
///
/// The ledger identity (Assets = Liabilities + Funds) holds because every <c>LedgerPosting</c> balances
/// by construction, not because this handler forces it — <see cref="StatementOfFinancialPositionDto.Difference"/>
/// is always computed and returned, never hidden or rounding-tolerant.</summary>
public sealed class GetStatementOfFinancialPositionQueryHandler(
    IFinancialStatementReportingService reportingService,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetStatementOfFinancialPositionQuery, StatementOfFinancialPositionDto>
{
    public async ValueTask<StatementOfFinancialPositionDto> Handle(
        GetStatementOfFinancialPositionQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        FinancialStatementSnapshot snapshot =
            await reportingService.GetAsOfSnapshotAsync(tenantId, asOfDate, query.FundId, cancellationToken);

        StatementOfFinancialPositionSectionDto assets = BuildSection(snapshot, AccountCategory.Asset);
        StatementOfFinancialPositionSectionDto liabilities = BuildSection(snapshot, AccountCategory.Liability);
        StatementOfFinancialPositionSectionDto funds = BuildSection(snapshot, AccountCategory.Equity);

        decimal cumulativeSurplusDeficit =
            snapshot.TotalFor(AccountCategory.Income) - snapshot.TotalFor(AccountCategory.Expense);

        decimal totalAssets = assets.Total;
        decimal totalLiabilities = liabilities.Total;
        decimal totalFunds = funds.Total + cumulativeSurplusDeficit;
        decimal totalLiabilitiesAndFunds = totalLiabilities + totalFunds;
        decimal difference = totalAssets - totalLiabilitiesAndFunds;

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, query.FundId is null ? "Tenant (all funds)" : "Single fund", clock.UtcNow, currentUser.UserId);

        List<UnmappedStatementAccountWarningDto> warnings = snapshot.UnmappedAccountWarnings
            .Select(w => new UnmappedStatementAccountWarningDto(
                w.ChartOfAccountId, w.Code, w.Name, w.Category, w.EffectiveStatementGroup, w.Amount))
            .ToList();

        return new StatementOfFinancialPositionDto(
            metadata, query.FundId, assets, liabilities, funds, cumulativeSurplusDeficit,
            totalAssets, totalLiabilities, totalFunds, totalLiabilitiesAndFunds, difference,
            IsBalanced: difference == 0m, warnings);
    }

    private static StatementOfFinancialPositionSectionDto BuildSection(
        FinancialStatementSnapshot snapshot, AccountCategory category)
    {
        List<StatementOfFinancialPositionGroupDto> groups = snapshot.Groups
            .Where(g => g.Category == category)
            .Select(g => new StatementOfFinancialPositionGroupDto(
                g.Group,
                g.Amount,
                g.Accounts
                    .Select(a => new StatementOfFinancialPositionAccountLineDto(a.ChartOfAccountId, a.Code, a.Name, a.Amount))
                    .ToList()))
            .ToList();

        return new StatementOfFinancialPositionSectionDto(groups, groups.Sum(g => g.Amount));
    }
}
