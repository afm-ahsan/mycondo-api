using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;

public sealed class BankReconciliationAlreadyReconciledException(BankReconciliationId id)
    : DomainException($"Bank reconciliation {id} is already reconciled.");
