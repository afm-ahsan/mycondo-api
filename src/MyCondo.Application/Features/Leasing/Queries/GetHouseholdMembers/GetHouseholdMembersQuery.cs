using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Queries.GetHouseholdMembers;

public sealed record GetHouseholdMembersQuery(Guid OccupancyRegistrationId) : IRequest<IReadOnlyList<HouseholdMemberDto>>;
