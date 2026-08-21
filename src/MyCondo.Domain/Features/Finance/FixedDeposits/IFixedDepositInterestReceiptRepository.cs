namespace MyCondo.Domain.Features.Finance.FixedDeposits;

/// <summary>One Fixed Deposit's non-reversed receipt totals (over whatever [fromDate, toDate]/all-time
/// window the caller asked for) — <see cref="TotalNetAmount"/> is derived (Gross − Deduction), never
/// independently entered, same rule as <see cref="FixedDepositInterestReceipt.NetAmount"/>.</summary>
public sealed record FixedDepositReceiptTotal(
    FixedDepositId FixedDepositId, int Count, decimal TotalGrossAmount, decimal TotalDeductionAmount)
{
    public decimal TotalNetAmount => TotalGrossAmount - TotalDeductionAmount;
}

public interface IFixedDepositInterestReceiptRepository
{
    Task<FixedDepositInterestReceipt?> GetByIdAsync(FixedDepositInterestReceiptId id, CancellationToken cancellationToken);

    Task<List<FixedDepositInterestReceipt>> GetForFixedDepositAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    Task<decimal> GetTotalReceivedGrossAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    /// <summary>Non-reversed receipt totals grouped by <see cref="FixedDeposit"/>, tenant-wide, server-
    /// side aggregated — see <see cref="IFixedDepositInterestAccrualRepository.GetTotalsByFixedDepositAsync"/>
    /// for the null-bound convention, filtered by <see cref="FixedDepositInterestReceipt.AccountingDate"/>.
    /// </summary>
    Task<IReadOnlyList<FixedDepositReceiptTotal>> GetTotalsByFixedDepositAsync(
        Guid tenantId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    void Add(FixedDepositInterestReceipt receipt);
}
