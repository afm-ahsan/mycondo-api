using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.SetHouseholdMemberPrimaryPhoto;

public sealed record SetHouseholdMemberPrimaryPhotoCommand(
    Guid HouseholdMemberId, Guid? AttachmentId
) : IRequest<HouseholdMemberDto>;
