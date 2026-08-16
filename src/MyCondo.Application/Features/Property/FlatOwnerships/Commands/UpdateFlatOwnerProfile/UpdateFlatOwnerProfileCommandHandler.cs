using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Application.Features.Residents.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;

public sealed class UpdateFlatOwnerProfileCommandHandler(
    IResidentRepository residents,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateFlatOwnerProfileCommandHandler> logger
) : IRequestHandler<UpdateFlatOwnerProfileCommand, ResidentDto>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<ResidentDto> Handle(UpdateFlatOwnerProfileCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId residentId = new(command.ResidentId);
        Resident resident = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), command.ResidentId);

        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), command.ResidentId);
        }

        Flat flat = await flats.GetByIdAsync(resident.FlatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), resident.FlatId.Value);

        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        resident.UpdateProfile(command.FullName, command.Phone, command.Email, clock.UtcNow);
        resident.UpdateOwnerDetails(
            command.AlternatePhone, command.NationalIdNumber, command.PassportNumber, command.DateOfBirth,
            command.Gender, command.PresentAddress, command.PermanentAddress, command.FatherName, command.MotherName,
            command.MaritalStatus, command.Profession, command.Employer, command.OfficeAddress,
            command.EmergencyContactName, command.EmergencyContactPhone, command.BloodGroup, command.Religion,
            command.Nationality, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Flat owner profile updated for resident {ResidentId}, tenant {TenantId}", residentId, tenantId);

        return resident.ToDto();
    }
}
