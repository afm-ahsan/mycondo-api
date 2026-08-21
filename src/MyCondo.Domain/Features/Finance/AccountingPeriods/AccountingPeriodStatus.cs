namespace MyCondo.Domain.Features.Finance.AccountingPeriods;

public enum AccountingPeriodStatus
{
    Open = 0,
    Closed = 1,

    /// <summary>Blocks new/original postings but still admits explicitly privileged adjustments
    /// (reversals, voids, waivers) while month-end review is in progress — see
    /// <c>FinancialPostingService.PostAsync</c>'s period-status check.</summary>
    SoftClosed = 2,
}
