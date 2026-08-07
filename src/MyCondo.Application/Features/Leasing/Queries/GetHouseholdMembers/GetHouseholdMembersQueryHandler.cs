using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Application.Features.Leasing.Queries.GetHouseholdMembers;

public sealed class GetHouseholdMembersQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IHouseholdMemberRepository members,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetHouseholdMembersQuery, IReadOnlyList<HouseholdMemberDto>>
{
    public async ValueTask<IReadOnlyList<HouseholdMemberDto>> Handle(
        GetHouseholdMembersQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId id = new(query.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        }

        IReadOnlyList<HouseholdMember> result = await members.GetForRegistrationAsync(id, cancellationToken);
        return result.Select(m => m.ToDto()).ToList();
    }
}
