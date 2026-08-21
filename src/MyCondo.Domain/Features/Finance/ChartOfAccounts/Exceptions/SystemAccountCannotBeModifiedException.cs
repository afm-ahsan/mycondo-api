using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.ChartOfAccounts.Exceptions;

public sealed class SystemAccountCannotBeModifiedException(ChartOfAccountId accountId)
    : DomainException($"Chart of account {accountId} is a system account backing an existing posting role and cannot be deactivated or reparented.");
