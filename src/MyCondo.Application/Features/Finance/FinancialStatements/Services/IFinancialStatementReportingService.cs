namespace MyCondo.Application.Features.Finance.FinancialStatements.Services;

/// <summary>The shared General-Ledger-based aggregation foundation for CondoBD Finance Phase 2A's
/// Financial Statements feature (Statement of Financial Position, Income &amp; Expenditure, Notes,
/// comparisons, and exports) — see <c>Phase 2A — Financial Statements Implementation Plan.md</c> §3.
/// Every figure originates exclusively from posted <c>LedgerEntry</c> rows resolved through
/// <c>ChartOfAccount.EffectiveStatementGroup</c> (Task 1); no statement built on top of this service may
/// derive totals from invoices, payments, expenses, fixed deposits, or other subledgers.</summary>
public interface IFinancialStatementReportingService
{
    /// <summary>Cumulative account balances through <paramref name="asOfDate"/> (inclusive) — the
    /// as-of-date semantics a Statement of Financial Position requires. See
    /// <c>ChartOfAccount.NormalBalance</c> for the sign convention applied to each account.</summary>
    /// <param name="tenantId">The tenant to report on — already resolved/authorized by the caller.</param>
    /// <param name="asOfDate">The as-of date; every posted entry with a business date on or before this
    /// date contributes.</param>
    /// <param name="fundId">When supplied, restricts to <c>LedgerEntry</c> rows tagged with this fund;
    /// omit for "All Funds".</param>
    /// <param name="cancellationToken"></param>
    Task<FinancialStatementSnapshot> GetAsOfSnapshotAsync(
        Guid tenantId, DateOnly asOfDate, Guid? fundId, CancellationToken cancellationToken);

    /// <summary>Account activity strictly within [<paramref name="startDate"/>, <paramref name="endDate"/>]
    /// (inclusive both ends) — the period semantics an Income &amp; Expenditure statement requires.
    /// Reversal postings net out automatically because they are ordinary additional ledger entries within
    /// the same window, not a special case this method has to detect.</summary>
    /// <param name="tenantId">The tenant to report on — already resolved/authorized by the caller.</param>
    /// <param name="startDate">First business date included in the window.</param>
    /// <param name="endDate">Last business date included in the window; must not be before
    /// <paramref name="startDate"/>.</param>
    /// <param name="fundId">When supplied, restricts to <c>LedgerEntry</c> rows tagged with this fund;
    /// omit for "All Funds".</param>
    /// <param name="cancellationToken"></param>
    Task<FinancialStatementSnapshot> GetPeriodSnapshotAsync(
        Guid tenantId, DateOnly startDate, DateOnly endDate, Guid? fundId, CancellationToken cancellationToken);
}
