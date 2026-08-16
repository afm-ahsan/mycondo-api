using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Leasing.Commands.CreateOccupancyRegistration;

public sealed class CreateOccupancyRegistrationCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IOccupancyRegistrationStatusHistoryRepository history,
    IFlatRepository flats,
    IResidentRepository residents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateOccupancyRegistrationCommandHandler> logger
) : IRequestHandler<CreateOccupancyRegistrationCommand, OccupancyRegistrationDto>
{
    public async ValueTask<OccupancyRegistrationDto> Handle(
        CreateOccupancyRegistrationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        OccupancyRegistration? existingActive = await registrations.GetActiveForFlatAsync(tenantId, flatId, cancellationToken);
        if (existingActive is not null)
        {
            throw new ConflictException("This flat already has an active tenant registration. Move it out before registering a new occupant.");
        }

        ResidentType occupancyType = Enum.Parse<ResidentType>(command.OccupancyType);

        Resident? resident = await residents.FindByFlatAndNameAsync(tenantId, flatId, command.PrimaryFullName, cancellationToken);
        if (resident is null)
        {
            resident = Resident.Register(
                tenantId, flatId, command.PrimaryFullName, command.PrimaryPhone, command.PrimaryEmail,
                occupancyType, clock.UtcNow);
            residents.Add(resident);
        }

        OccupancyRegistration registration = OccupancyRegistration.Register(
            tenantId, flatId, resident.Id, occupancyType, command.PrimaryFullName, command.PrimaryPhone,
            command.PrimaryEmail, command.PrimaryNationalIdNumber, command.PrimaryDateOfBirth, command.PrimaryGender,
            command.PrimaryBloodGroup, command.PrimaryReligion, command.PrimaryNationality,
            command.PrimaryProfession, command.PrimaryPermanentAddress, command.EmergencyContactName,
            command.EmergencyContactPhone, command.MoveInExpectedDate, clock.UtcNow);

        registrations.Add(registration);
        history.Add(OccupancyRegistrationStatusHistory.Record(
            tenantId, registration.Id, null, OccupancyRegistrationStatus.Draft, currentUser.UserId, null, clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Occupancy registration {OccupancyRegistrationId} created for flat {FlatId}, tenant {TenantId}",
            registration.Id, flatId, tenantId);

        return registration.ToDto();
    }
}
