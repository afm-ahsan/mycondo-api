using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.DeactivateHouseholdMember;

public sealed record DeactivateHouseholdMemberCommand(Guid HouseholdMemberId) : IRequest<HouseholdMemberDto>;
