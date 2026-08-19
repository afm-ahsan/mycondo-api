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

    void Add(FixedDeposit fixedDeposit);
}
