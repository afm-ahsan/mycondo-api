using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FixedDepositRepository(MyCondoDbContext db) : IFixedDepositRepository
{
    public Task<FixedDeposit?> GetByIdAsync(FixedDepositId id, CancellationToken cancellationToken) =>
        db.Set<FixedDeposit>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsForCertificateNumberAsync(Guid tenantId, string certificateNumber, CancellationToken cancellationToken) =>
        db.Set<FixedDeposit>().AnyAsync(x => x.TenantId == tenantId && x.CertificateNumber == certificateNumber, cancellationToken);

    public async Task<PagedResult<FixedDeposit>> SearchAsync(
        Guid tenantId,
        FixedDepositStatus? status,
        FundId? fundId,
        FinancialAccountId? fundingFinancialAccountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<FixedDeposit> query = db.Set<FixedDeposit>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (fundId is not null)
        {
            query = query.Where(x => x.FundId == fundId);
        }

        if (fundingFinancialAccountId is not null)
        {
            query = query.Where(x => x.FundingFinancialAccountId == fundingFinancialAccountId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<FixedDeposit> items = await query
            .OrderByDescending(x => x.StartDate).ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FixedDeposit>(items, page, pageSize, total);
    }

    public void Add(FixedDeposit fixedDeposit) => db.Set<FixedDeposit>().Add(fixedDeposit);
}
