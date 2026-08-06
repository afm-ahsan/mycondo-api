using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class LedgerPostingRepository(MyCondoDbContext db) : ILedgerPostingRepository
{
    public void Add(LedgerPosting posting) => db.Set<LedgerPosting>().Add(posting);
}
