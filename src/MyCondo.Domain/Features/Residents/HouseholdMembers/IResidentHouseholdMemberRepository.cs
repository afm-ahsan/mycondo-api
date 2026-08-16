namespace MyCondo.Domain.Features.Residents.HouseholdMembers;

public interface IResidentHouseholdMemberRepository
{
    Task<ResidentHouseholdMember?> GetByIdAsync(ResidentHouseholdMemberId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ResidentHouseholdMember>> GetForResidentAsync(
        Guid residentId, CancellationToken cancellationToken);

    void Add(ResidentHouseholdMember member);
}
