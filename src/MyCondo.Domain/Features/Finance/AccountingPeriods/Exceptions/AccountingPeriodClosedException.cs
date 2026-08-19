using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;

/// <summary>Thrown by the Application-layer centralized posting service when a posting's business date
/// falls inside a closed period — closed-period posting prevention is the posting service's
/// responsibility, not this aggregate's, since it requires resolving which period a date belongs to.
/// This aggregate only enforces its own already-closed re-close guard.</summary>
public sealed class AccountingPeriodClosedException(AccountingPeriodId id, DateOnly businessDate)
    : DomainException($"Accounting period {id} covering {businessDate} is closed; postings cannot be made against it.");
