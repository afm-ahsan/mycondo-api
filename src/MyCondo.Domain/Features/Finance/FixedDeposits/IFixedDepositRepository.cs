using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Domain.Features.Finance.FixedDeposits;

public interface IFixedDepositRepository
{
    Task<FixedDeposit?> GetByIdAsync(FixedDepositId id, CancellationToken cancellationToken);

    Task<bool> ExistsForCertificateNumberAsync(Guid tenantId, string certificateNumber, CancellationToken cancellationToken);

    Task<PagedResult<FixedDeposit>> SearchAsync(
        Guid tenantId,
        FixedDepositStatus? status,
        FundId? fundId,
        FinancialAccountId? fundingFinancialAccountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Sum of <see cref="FixedDeposit.Principal"/> across every <see cref="FixedDepositStatus.Active"/>
    /// instrument — the Financial Overview report's "Fixed Deposit Principal" figure. Computed from the
    /// FixedDeposit entities themselves, not the ledger, because the tenant-wide <c>FixedDeposit</c>
    /// ledger account is shared across every instrument (Template 4) — per-instrument principal is only
    /// available here.</summary>
    Task<decimal> GetActivePrincipalTotalAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Unpaginated, every status — the Fixed Deposit Portfolio report needs every instrument
    /// (Active/Renewed/Withdrawn, so its status column is meaningful) in one pass, not one page of them
    /// (mirrors <c>IExpenseCategoryRepository.GetAllForTenantAsync</c>'s rationale). Voided instruments
    /// (data-entry corrections, never real principal — see <c>FixedDeposit.Void</c>) are excluded.
    /// </summary>
    Task<List<FixedDeposit>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(FixedDeposit fixedDeposit);
}
