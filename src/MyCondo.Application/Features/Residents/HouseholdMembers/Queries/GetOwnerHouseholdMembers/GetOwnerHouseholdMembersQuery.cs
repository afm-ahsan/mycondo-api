using Mediator;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Queries.GetOwnerHouseholdMembers;

public sealed record GetOwnerHouseholdMembersQuery(Guid ResidentId) : IRequest<IReadOnlyList<ResidentHouseholdMemberDto>>;
