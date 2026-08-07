using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class HouseholdMemberRepository(MyCondoDbContext db) : IHouseholdMemberRepository
{
    public Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken) =>
        db.Set<HouseholdMember>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>Tracked (not <c>AsNoTracking</c>) — used both for display and for cascade mutations
    /// like move-out, which deactivate every member in the same unit of work.</summary>
    public async Task<IReadOnlyList<HouseholdMember>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken) =>
        await db.Set<HouseholdMember>()
            .Where(x => x.OccupancyRegistrationId == occupancyRegistrationId)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

    public void Add(HouseholdMember member) => db.Set<HouseholdMember>().Add(member);
}
