namespace MyCondo.Domain.Features.Finance.FixedDeposits;

/// <summary>One Fixed Deposit's non-reversed accrual total (over whatever [fromDate, toDate]/all-time
/// window the caller asked for) — the Fixed Deposit Portfolio (all-time) and Fixed Deposit Interest
/// (period-scoped) reports' per-instrument accrued figure.</summary>
public sealed record FixedDepositAccrualTotal(FixedDepositId FixedDepositId, int Count, decimal TotalGrossAmount);

public interface IFixedDepositInterestAccrualRepository
{
    Task<FixedDepositInterestAccrual?> GetByIdAsync(FixedDepositInterestAccrualId id, CancellationToken cancellationToken);

    Task<List<FixedDepositInterestAccrual>> GetForFixedDepositAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    Task<decimal> GetTotalAccruedAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    /// <summary>Non-reversed accrual totals grouped by <see cref="FixedDeposit"/>, tenant-wide, server-
    /// side aggregated (SQL <c>GROUP BY</c>/<c>SUM</c>) — avoids one query per instrument. Null
    /// <paramref name="fromDate"/>/<paramref name="toDate"/> means unbounded on that side (Portfolio's
    /// all-time snapshot passes both null; the Interest report's period figure bounds both; the
    /// Interest report's cumulative-outstanding figure bounds only <paramref name="toDate"/>), filtered
    /// by <see cref="FixedDepositInterestAccrual.AccountingDate"/>.</summary>
    Task<IReadOnlyList<FixedDepositAccrualTotal>> GetTotalsByFixedDepositAsync(
        Guid tenantId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    void Add(FixedDepositInterestAccrual accrual);
}
