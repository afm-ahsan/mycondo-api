using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Domain.Features.Finance.BankReconciliations;

public interface IBankReconciliationRepository
{
    void Add(BankReconciliation reconciliation);

    Task<BankReconciliation?> GetByIdAsync(BankReconciliationId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BankReconciliation>> GetForFinancialAccountAsync(
        FinancialAccountId financialAccountId, CancellationToken cancellationToken);
}
