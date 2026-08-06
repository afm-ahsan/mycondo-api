using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ResidentAccountRepository(MyCondoDbContext db) : IResidentAccountRepository
{
    public Task<ResidentAccount?> GetByIdAsync(ResidentAccountId id, CancellationToken cancellationToken) =>
        db.Set<ResidentAccount>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<ResidentAccount?> GetByFlatIdAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken) =>
        db.Set<ResidentAccount>().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.FlatId == flatId, cancellationToken);

    public void Add(ResidentAccount account) => db.Set<ResidentAccount>().Add(account);
}
