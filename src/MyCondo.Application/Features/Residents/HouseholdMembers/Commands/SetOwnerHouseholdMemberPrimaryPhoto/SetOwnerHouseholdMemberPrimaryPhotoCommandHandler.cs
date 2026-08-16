using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Application.Features.Residents.HouseholdMembers.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Commands.SetOwnerHouseholdMemberPrimaryPhoto;

public sealed class SetOwnerHouseholdMemberPrimaryPhotoCommandHandler(
    IResidentHouseholdMemberRepository members,
    IResidentRepository residents,
    IFlatRepository flats,
    IAttachmentRepository attachments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetOwnerHouseholdMemberPrimaryPhotoCommandHandler> logger
) : IRequestHandler<SetOwnerHouseholdMemberPrimaryPhotoCommand, ResidentHouseholdMemberDto>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<ResidentHouseholdMemberDto> Handle(
        SetOwnerHouseholdMemberPrimaryPhotoCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentHouseholdMemberId id = new(command.ResidentHouseholdMemberId);
        ResidentHouseholdMember member = await members.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ResidentHouseholdMember), command.ResidentHouseholdMemberId);
        if (member.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ResidentHouseholdMember), command.ResidentHouseholdMemberId);
        }

        Resident resident = await residents.GetByIdAsync(new ResidentId(member.ResidentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), member.ResidentId);
        Flat flat = await flats.GetByIdAsync(resident.FlatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), resident.FlatId.Value);
        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        if (command.AttachmentId is Guid attachmentId)
        {
            Attachment attachment = await attachments.GetByIdAsync(new AttachmentId(attachmentId), cancellationToken)
                ?? throw new NotFoundException(nameof(Attachment), attachmentId);
            if (attachment.TenantId != tenantId
                || attachment.OwnerType != AttachmentOwnerType.ResidentHouseholdMember
                || attachment.OwnerId != command.ResidentHouseholdMemberId)
            {
                throw new NotFoundException(nameof(Attachment), attachmentId);
            }
        }

        member.SetPrimaryPhoto(command.AttachmentId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Owner household member {ResidentHouseholdMemberId} primary photo set to attachment {AttachmentId}, tenant {TenantId}",
            id, command.AttachmentId, tenantId);

        return member.ToDto();
    }
}
