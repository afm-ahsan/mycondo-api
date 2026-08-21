using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;

/// <summary>Thrown by the centralized posting service when a posting's business date falls inside a
/// soft-closed period and the posting is not an explicitly privileged adjustment (reversal/void/waiver —
/// see <c>FinancialPostingRequest.IsPrivilegedAdjustment</c>). Soft-closed periods block new/original
/// charges while month-end review is in progress but still allow corrections to what was already
/// recorded, matching the Template 6 governance requirement that soft-close only admit "explicitly
/// privileged adjustments".</summary>
public sealed class AccountingPeriodSoftClosedException(AccountingPeriodId id, DateOnly businessDate)
    : DomainException($"Accounting period {id} covering {businessDate} is soft-closed; only privileged adjustments are allowed.");
