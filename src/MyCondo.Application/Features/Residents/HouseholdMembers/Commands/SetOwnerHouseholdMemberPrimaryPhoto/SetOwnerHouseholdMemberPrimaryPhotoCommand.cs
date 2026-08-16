using Mediator;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Commands.SetOwnerHouseholdMemberPrimaryPhoto;

public sealed record SetOwnerHouseholdMemberPrimaryPhotoCommand(
    Guid ResidentHouseholdMemberId, Guid? AttachmentId
) : IRequest<ResidentHouseholdMemberDto>;
