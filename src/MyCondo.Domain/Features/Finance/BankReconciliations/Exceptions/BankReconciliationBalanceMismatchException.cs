using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;

/// <summary>Thrown when the computed ledger-side reconciled balance does not equal the entered bank
/// statement balance — a genuine unresolved discrepancy (unmatched item, timing difference not yet
/// carried forward, or a real error) blocks completion rather than being silently accepted.</summary>
public sealed class BankReconciliationBalanceMismatchException(BankReconciliationId id, decimal statementBalance, decimal computedBalance)
    : DomainException(
        $"Bank reconciliation {id} does not balance: statement balance {statementBalance} vs. computed ledger balance {computedBalance}.");
