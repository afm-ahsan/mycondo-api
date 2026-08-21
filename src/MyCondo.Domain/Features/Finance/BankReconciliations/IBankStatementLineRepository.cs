namespace MyCondo.Domain.Features.Finance.BankReconciliations;

public interface IBankStatementLineRepository
{
    void Add(BankStatementLine line);

    Task<BankStatementLine?> GetByIdAsync(BankStatementLineId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BankStatementLine>> GetForReconciliationAsync(
        BankReconciliationId bankReconciliationId, CancellationToken cancellationToken);

    Task<bool> HasUnresolvedLinesAsync(BankReconciliationId bankReconciliationId, CancellationToken cancellationToken);
}
