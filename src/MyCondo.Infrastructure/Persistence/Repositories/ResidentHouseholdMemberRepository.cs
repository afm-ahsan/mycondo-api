using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ResidentHouseholdMemberRepository(MyCondoDbContext db) : IResidentHouseholdMemberRepository
{
    public Task<ResidentHouseholdMember?> GetByIdAsync(
        ResidentHouseholdMemberId id, CancellationToken cancellationToken) =>
        db.Set<ResidentHouseholdMember>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ResidentHouseholdMember>> GetForResidentAsync(
        Guid residentId, CancellationToken cancellationToken) =>
        await db.Set<ResidentHouseholdMember>()
            .Where(x => x.ResidentId == residentId)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

    public void Add(ResidentHouseholdMember member) => db.Set<ResidentHouseholdMember>().Add(member);
}
