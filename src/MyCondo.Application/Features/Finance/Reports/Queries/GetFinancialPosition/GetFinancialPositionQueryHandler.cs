using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;

/// <summary>A Balance Sheet derived from the same Trial Balance data as
/// <c>GetTrialBalanceQueryHandler</c>, not a second aggregation path. There is no period-end closing
/// step in this system (no evidence one exists across Templates 1-4), so Income/Expense activity
/// up to <see cref="GetFinancialPositionQuery.AsOfDate"/> is folded into
/// <see cref="FinancialPositionReportDto.RetainedSurplusDeficit"/> as an implicit "current period"
/// retained-earnings line — the ledger-balance identity
/// (Assets = Liabilities + Equity + RetainedSurplusDeficit) holds structurally because every posting
/// balances (see <c>LedgerPosting.Create</c>), not by construction in this handler.</summary>
public sealed class GetFinancialPositionQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFinancialPositionQuery, FinancialPositionReportDto>
{
    public async ValueTask<FinancialPositionReportDto> Handle(GetFinancialPositionQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        IReadOnlyList<TrialBalanceAccountLine> lines = await reports.GetTrialBalanceAsync(tenantId, asOfDate, cancellationToken);

        FinancialPositionSectionDto BuildSection(AccountCategory category, bool debitPositive) =>
            BuildSectionInternal(lines, category, debitPositive);

        FinancialPositionSectionDto assets = BuildSection(AccountCategory.Asset, debitPositive: true);
        FinancialPositionSectionDto liabilities = BuildSection(AccountCategory.Liability, debitPositive: false);
        FinancialPositionSectionDto equity = BuildSection(AccountCategory.Equity, debitPositive: false);

        decimal totalIncome = lines.Where(l => l.Category == AccountCategory.Income).Sum(l => l.TotalCredit - l.TotalDebit);
        decimal totalExpense = lines.Where(l => l.Category == AccountCategory.Expense).Sum(l => l.TotalDebit - l.TotalCredit);
        decimal retainedSurplusDeficit = totalIncome - totalExpense;

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, "Tenant (all accounts)", clock.UtcNow, currentUser.UserId);

        return new FinancialPositionReportDto(
            metadata, assets, liabilities, equity, retainedSurplusDeficit,
            liabilities.Total + equity.Total + retainedSurplusDeficit);
    }

    private static FinancialPositionSectionDto BuildSectionInternal(
        IReadOnlyList<TrialBalanceAccountLine> lines, AccountCategory category, bool debitPositive)
    {
        List<FinancialPositionLineDto> sectionLines = lines
            .Where(l => l.Category == category)
            .Select(l => new FinancialPositionLineDto(
                l.ChartOfAccountId.Value, l.Code, l.Name,
                debitPositive ? l.TotalDebit - l.TotalCredit : l.TotalCredit - l.TotalDebit))
            .Where(l => l.Balance != 0m)
            .OrderBy(l => l.Code)
            .ToList();

        return new FinancialPositionSectionDto(sectionLines, sectionLines.Sum(l => l.Balance));
    }
}
