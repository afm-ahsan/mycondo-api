using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Attachments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Application.Features.Attachments.Commands.UploadAttachment;

/// <summary>
/// Persists uploaded file bytes via <see cref="IFileStorageService"/> before recording metadata —
/// StorageKey is always server-generated (see <see cref="IFileStorageService.SaveAsync"/>), never
/// caller-supplied, so an attachment record can never point at a file the caller doesn't actually own.
/// </summary>
public sealed class UploadAttachmentCommandHandler(
    IAttachmentRepository attachments,
    IResidentRepository residents,
    IOccupancyRegistrationRepository occupancyRegistrations,
    IBuildingRepository buildings,
    IFlatRepository flats,
    IResidentHouseholdMemberRepository residentHouseholdMembers,
    IHouseholdMemberRepository leasingHouseholdMembers,
    IFileStorageService fileStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UploadAttachmentCommandHandler> logger
) : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async ValueTask<AttachmentDto> Handle(UploadAttachmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AttachmentOwnerType ownerType = Enum.Parse<AttachmentOwnerType>(command.OwnerType);

        await EnsureOwnerExistsForTenantAsync(ownerType, command.OwnerId, tenantId, cancellationToken);

        string storageKey = await fileStorage.SaveAsync(
            command.Content, command.FileName, command.ContentType, cancellationToken);

        Attachment attachment = Attachment.Record(
            tenantId, ownerType, command.OwnerId, storageKey, command.FileName, command.ContentType,
            command.SizeBytes, clock.UtcNow);

        attachments.Add(attachment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Attachment {AttachmentId} uploaded for {OwnerType} {OwnerId}, tenant {TenantId}",
            attachment.Id, ownerType, command.OwnerId, tenantId);

        return new AttachmentDto(
            attachment.Id.Value, attachment.OwnerType.ToString(), attachment.OwnerId, attachment.StorageKey,
            attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedAtUtc);
    }

    private async Task EnsureOwnerExistsForTenantAsync(
        AttachmentOwnerType ownerType, Guid ownerId, Guid tenantId, CancellationToken cancellationToken)
    {
        switch (ownerType)
        {
            case AttachmentOwnerType.Resident:
                Resident resident = await residents.GetByIdAsync(new ResidentId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(Resident), ownerId);
                if (resident.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(Resident), ownerId);
                }

                break;
            case AttachmentOwnerType.OccupancyRegistration:
                OccupancyRegistration registration = await occupancyRegistrations.GetByIdAsync(
                        new OccupancyRegistrationId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(OccupancyRegistration), ownerId);
                if (registration.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(OccupancyRegistration), ownerId);
                }

                break;
            case AttachmentOwnerType.Building:
                Building building = await buildings.GetByIdAsync(new BuildingId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(Building), ownerId);
                if (building.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(Building), ownerId);
                }

                break;
            case AttachmentOwnerType.Flat:
                Flat flat = await flats.GetByIdAsync(new FlatId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(Flat), ownerId);
                if (flat.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(Flat), ownerId);
                }

                break;
            case AttachmentOwnerType.ResidentHouseholdMember:
                ResidentHouseholdMember residentHouseholdMember = await residentHouseholdMembers.GetByIdAsync(
                        new ResidentHouseholdMemberId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(ResidentHouseholdMember), ownerId);
                if (residentHouseholdMember.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(ResidentHouseholdMember), ownerId);
                }

                break;
            case AttachmentOwnerType.LeasingHouseholdMember:
                HouseholdMember leasingHouseholdMember = await leasingHouseholdMembers.GetByIdAsync(
                        new HouseholdMemberId(ownerId), cancellationToken)
                    ?? throw new NotFoundException(nameof(HouseholdMember), ownerId);
                if (leasingHouseholdMember.TenantId != tenantId)
                {
                    throw new NotFoundException(nameof(HouseholdMember), ownerId);
                }

                break;
            default:
                throw new NotFoundException(nameof(Attachment), ownerId);
        }
    }
}
