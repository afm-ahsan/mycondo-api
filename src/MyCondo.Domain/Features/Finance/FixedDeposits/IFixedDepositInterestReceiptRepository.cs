namespace MyCondo.Domain.Features.Finance.FixedDeposits;

public interface IFixedDepositInterestReceiptRepository
{
    Task<FixedDepositInterestReceipt?> GetByIdAsync(FixedDepositInterestReceiptId id, CancellationToken cancellationToken);

    Task<List<FixedDepositInterestReceipt>> GetForFixedDepositAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    Task<decimal> GetTotalReceivedGrossAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    void Add(FixedDepositInterestReceipt receipt);
}
