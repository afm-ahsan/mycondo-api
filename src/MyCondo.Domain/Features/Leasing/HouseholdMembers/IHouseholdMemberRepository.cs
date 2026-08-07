using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.Features.Leasing.HouseholdMembers;

public interface IHouseholdMemberRepository
{
    Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<HouseholdMember>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken);

    void Add(HouseholdMember member);
}
