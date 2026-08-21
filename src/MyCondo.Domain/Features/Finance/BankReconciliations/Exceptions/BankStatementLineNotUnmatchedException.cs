using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;

public sealed class BankStatementLineNotUnmatchedException(BankStatementLineId id, BankStatementLineStatus status)
    : DomainException($"Bank statement line {id} is {status}, not Unmatched — it has already been resolved.");
