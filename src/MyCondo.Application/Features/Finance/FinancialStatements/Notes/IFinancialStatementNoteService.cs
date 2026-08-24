namespace MyCondo.Application.Features.Finance.FinancialStatements.Notes;

/// <summary>
/// Builds CondoBD Finance Phase 2A Task 5's supporting schedules ("Notes to Accounts").
///
/// The model is strictly one-directional:
/// <c>Primary Financial Statement → General Ledger balance → Supporting Schedule → subledger records</c>.
/// A note never contributes a figure back to a primary statement, never replaces a GL total with a
/// subledger total, and never adjusts either side to make them agree — see
/// <see cref="FinancialStatementNote.Difference"/>.
/// </summary>
public interface IFinancialStatementNoteService
{
    /// <summary>Notes explaining Statement of Financial Position balances — every figure is cumulative
    /// through <paramref name="asOfDate"/> (inclusive), reconstructed from posted ledger entries rather
    /// than from any subledger's current state.</summary>
    /// <param name="tenantId">The tenant to report on — already resolved/authorized by the caller.</param>
    /// <param name="asOfDate">The statement date; every posted entry on or before it contributes.</param>
    /// <param name="fundId">When supplied, restricts to ledger entries tagged with this fund — the same
    /// filter the parent statement applied, so a fund-scoped statement never gets an
    /// organization-wide schedule.</param>
    /// <param name="keys">The schedules to build; empty means "every as-of schedule".</param>
    /// <param name="cancellationToken"></param>
    Task<IReadOnlyList<FinancialStatementNote>> GetAsOfNotesAsync(
        Guid tenantId,
        DateOnly asOfDate,
        Guid? fundId,
        IReadOnlyCollection<FinancialStatementNoteKey> keys,
        CancellationToken cancellationToken);

    /// <summary>Notes explaining Income &amp; Expenditure activity within
    /// [<paramref name="startDate"/>, <paramref name="endDate"/>] (inclusive both ends). Reversals net
    /// out naturally because they are ordinary additional ledger entries in the same window.</summary>
    Task<IReadOnlyList<FinancialStatementNote>> GetPeriodNotesAsync(
        Guid tenantId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? fundId,
        IReadOnlyCollection<FinancialStatementNoteKey> keys,
        CancellationToken cancellationToken);

    /// <summary>The schedules available for each statement — the single source of truth a caller/UI uses
    /// instead of hardcoding which note belongs where.</summary>
    static IReadOnlyList<FinancialStatementNoteKey> AsOfNoteKeys { get; } =
    [
        FinancialStatementNoteKey.CashAndBank,
        FinancialStatementNoteKey.FixedDeposits,
        FinancialStatementNoteKey.ServiceChargeReceivable,
        FinancialStatementNoteKey.ResidentAdvances,
        FinancialStatementNoteKey.Payables,
    ];

    static IReadOnlyList<FinancialStatementNoteKey> PeriodNoteKeys { get; } =
    [
        FinancialStatementNoteKey.InterestIncome,
        FinancialStatementNoteKey.OperatingExpensesByCategory,
    ];
}
