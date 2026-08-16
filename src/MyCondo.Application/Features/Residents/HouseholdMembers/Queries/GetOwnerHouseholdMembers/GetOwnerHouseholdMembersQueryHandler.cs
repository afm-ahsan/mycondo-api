using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Application.Features.Residents.HouseholdMembers.Mappings;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Queries.GetOwnerHouseholdMembers;

public sealed class GetOwnerHouseholdMembersQueryHandler(
    IResidentRepository residents,
    IResidentHouseholdMemberRepository members,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOwnerHouseholdMembersQuery, IReadOnlyList<ResidentHouseholdMemberDto>>
{
    public async ValueTask<IReadOnlyList<ResidentHouseholdMemberDto>> Handle(
        GetOwnerHouseholdMembersQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId residentId = new(query.ResidentId);
        Resident resident = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), query.ResidentId);
        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), query.ResidentId);
        }

        IReadOnlyList<ResidentHouseholdMember> result = await members.GetForResidentAsync(query.ResidentId, cancellationToken);
        return result.Select(m => m.ToDto()).ToList();
    }
}
