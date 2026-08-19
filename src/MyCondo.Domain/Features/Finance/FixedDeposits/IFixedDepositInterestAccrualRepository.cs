namespace MyCondo.Domain.Features.Finance.FixedDeposits;

public interface IFixedDepositInterestAccrualRepository
{
    Task<FixedDepositInterestAccrual?> GetByIdAsync(FixedDepositInterestAccrualId id, CancellationToken cancellationToken);

    Task<List<FixedDepositInterestAccrual>> GetForFixedDepositAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    Task<decimal> GetTotalAccruedAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken);

    void Add(FixedDepositInterestAccrual accrual);
}
