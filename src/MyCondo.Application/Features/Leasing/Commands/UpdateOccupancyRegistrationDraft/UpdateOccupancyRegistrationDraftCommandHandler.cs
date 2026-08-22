using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Application.Features.Leasing.Commands.UpdateOccupancyRegistrationDraft;

public sealed class UpdateOccupancyRegistrationDraftCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateOccupancyRegistrationDraftCommandHandler> logger
) : IRequestHandler<UpdateOccupancyRegistrationDraftCommand, OccupancyRegistrationDto>
{
    public async ValueTask<OccupancyRegistrationDto> Handle(
        UpdateOccupancyRegistrationDraftCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId id = new(command.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        }

        registration.UpdateDraft(
            command.PrimaryFullName, command.PrimaryPhone, command.PrimaryEmail, command.PrimaryNationalIdNumber,
            command.PrimaryDateOfBirth, command.PrimaryGender, command.PrimaryBloodGroup, command.PrimaryReligion,
            command.PrimaryNationality, command.PrimaryFatherName, command.PrimaryMotherName,
            command.PrimaryMaritalStatus, command.PrimaryProfession, command.PrimaryPermanentAddress,
            command.EmergencyContactName, command.EmergencyContactPhone, command.MoveInExpectedDate);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Occupancy registration {OccupancyRegistrationId} draft updated, tenant {TenantId}", id, tenantId);

        return registration.ToDto();
    }
}
