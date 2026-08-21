namespace MyCondo.Application.Features.Finance.Integrity.DTOs;

/// <summary>Every count is expected to be zero — see <c>IFinanceIntegrityRepository</c>'s doc comment.
/// <see cref="IsHealthy"/> is true only when every data-integrity count (not the operational-backlog
/// counts, which are expected to be non-zero in normal day-to-day use) is zero.</summary>
public sealed record FinancialIntegrityDashboardDto(
    long UnbalancedPostingsCount,
    long DuplicateLogicalPostingsCount,
    long ClosedPeriodViolationsCount,
    long StaleUnreconciledBankItemsCount,
    long StaleUnreceivedInterestAccrualsCount,
    bool IsHealthy);
