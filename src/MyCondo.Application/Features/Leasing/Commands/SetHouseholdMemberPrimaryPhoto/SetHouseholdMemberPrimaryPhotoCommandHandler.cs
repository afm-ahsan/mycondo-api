using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;

namespace MyCondo.Application.Features.Leasing.Commands.SetHouseholdMemberPrimaryPhoto;

public sealed class SetHouseholdMemberPrimaryPhotoCommandHandler(
    IHouseholdMemberRepository members,
    IAttachmentRepository attachments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetHouseholdMemberPrimaryPhotoCommandHandler> logger
) : IRequestHandler<SetHouseholdMemberPrimaryPhotoCommand, HouseholdMemberDto>
{
    public async ValueTask<HouseholdMemberDto> Handle(
        SetHouseholdMemberPrimaryPhotoCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        HouseholdMemberId id = new(command.HouseholdMemberId);
        HouseholdMember member = await members.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdMember), command.HouseholdMemberId);
        if (member.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(HouseholdMember), command.HouseholdMemberId);
        }

        if (command.AttachmentId is Guid attachmentId)
        {
            Attachment attachment = await attachments.GetByIdAsync(new AttachmentId(attachmentId), cancellationToken)
                ?? throw new NotFoundException(nameof(Attachment), attachmentId);
            if (attachment.TenantId != tenantId
                || attachment.OwnerType != AttachmentOwnerType.LeasingHouseholdMember
                || attachment.OwnerId != command.HouseholdMemberId)
            {
                throw new NotFoundException(nameof(Attachment), attachmentId);
            }
        }

        member.SetPrimaryPhoto(command.AttachmentId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Household member {HouseholdMemberId} primary photo set to attachment {AttachmentId}, tenant {TenantId}",
            id, command.AttachmentId, tenantId);

        return member.ToDto();
    }
}
