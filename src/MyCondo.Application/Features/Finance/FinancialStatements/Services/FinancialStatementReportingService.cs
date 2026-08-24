using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Services;

/// <inheritdoc cref="IFinancialStatementReportingService"/>
public sealed class FinancialStatementReportingService(IFinanceReportRepository reports) : IFinancialStatementReportingService
{
    public async Task<FinancialStatementSnapshot> GetAsOfSnapshotAsync(
        Guid tenantId, DateOnly asOfDate, Guid? fundId, CancellationToken cancellationToken)
    {
        IReadOnlyList<StatementAccountActivityLine> lines =
            await reports.GetStatementBalancesAsOfAsync(tenantId, asOfDate, fundId, cancellationToken);

        return BuildSnapshot(lines);
    }

    public async Task<FinancialStatementSnapshot> GetPeriodSnapshotAsync(
        Guid tenantId, DateOnly startDate, DateOnly endDate, Guid? fundId, CancellationToken cancellationToken)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("endDate must not be before startDate.", nameof(endDate));
        }

        IReadOnlyList<StatementAccountActivityLine> lines =
            await reports.GetStatementActivityForPeriodAsync(tenantId, startDate, endDate, fundId, cancellationToken);

        return BuildSnapshot(lines);
    }

    /// <summary>Nets each account to its <c>NormalBalance</c>-signed amount, drops zero-balance/zero-
    /// activity accounts (consistent with every existing Finance report's zero-line convention), groups
    /// the remainder by <see cref="ChartOfAccount.EffectiveStatementGroup"/>, and surfaces an
    /// <see cref="UnmappedAccountWarning"/> for every non-zero account with no explicit
    /// <c>StatementGroup</c> — the fallback bucket keeps it visible on the statement; the warning is what
    /// lets a later UI/report flag it for classification (Phase 2A plan §8/§15).</summary>
    private static FinancialStatementSnapshot BuildSnapshot(IReadOnlyList<StatementAccountActivityLine> lines)
    {
        List<UnmappedAccountWarning> warnings = [];
        List<FinancialStatementGroupAmount> groups = [];

        foreach (IGrouping<FinancialStatementGroup, StatementAccountActivityLine> groupLines in
            lines.GroupBy(l => l.EffectiveStatementGroup))
        {
            List<FinancialStatementAccountAmount> accounts = [];

            foreach (StatementAccountActivityLine line in groupLines)
            {
                decimal amount = line.NormalBalance == LedgerDirection.Debit
                    ? line.TotalDebit - line.TotalCredit
                    : line.TotalCredit - line.TotalDebit;

                if (amount == 0m)
                {
                    continue;
                }

                accounts.Add(new FinancialStatementAccountAmount(line.ChartOfAccountId.Value, line.Code, line.Name, amount));

                if (line.StatementGroup is null)
                {
                    warnings.Add(new UnmappedAccountWarning(
                        line.ChartOfAccountId.Value, line.Code, line.Name, line.Category, line.EffectiveStatementGroup, amount));
                }
            }

            if (accounts.Count == 0)
            {
                continue;
            }

            groups.Add(new FinancialStatementGroupAmount(
                groupLines.Key,
                FinancialStatementGroupCategories.CategoryOf(groupLines.Key),
                accounts.Sum(a => a.Amount),
                accounts.OrderBy(a => a.Code).ToList()));
        }

        return new FinancialStatementSnapshot(
            groups.OrderBy(g => g.Group).ToList(),
            warnings.OrderBy(w => w.Code).ToList());
    }
}
