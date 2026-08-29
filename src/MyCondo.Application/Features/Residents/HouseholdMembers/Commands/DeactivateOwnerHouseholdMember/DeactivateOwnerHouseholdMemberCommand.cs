using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Commands.DeactivateOwnerHouseholdMember;

public sealed record DeactivateOwnerHouseholdMemberCommand(Guid ResidentHouseholdMemberId) : IRequest<ResidentHouseholdMemberDto>, ILifecycleWriteOperation;
