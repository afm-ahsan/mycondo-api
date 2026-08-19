using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;

public sealed class AccountingPeriodAlreadyClosedException(AccountingPeriodId id)
    : DomainException($"Accounting period {id} is already closed.");
