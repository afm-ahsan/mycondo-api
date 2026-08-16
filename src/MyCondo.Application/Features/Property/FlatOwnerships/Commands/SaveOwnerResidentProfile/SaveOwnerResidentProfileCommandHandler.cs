using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Application.Features.Residents.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.SaveOwnerResidentProfile;

public sealed class SaveOwnerResidentProfileCommandHandler(
    IResidentRepository residents,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<SaveOwnerResidentProfileCommandHandler> logger
) : IRequestHandler<SaveOwnerResidentProfileCommand, ResidentDto>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<ResidentDto> Handle(SaveOwnerResidentProfileCommand command, CancellationToken cancellationToken)
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

        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        string fullName = command.FullName.Trim();

        Resident? resident = await residents.FindByFlatAndNameAsync(tenantId, flatId, fullName, cancellationToken);
        if (resident is null)
        {
            resident = Resident.Register(
                tenantId, flatId, fullName, command.Phone, command.Email, ResidentType.Owner, clock.UtcNow);
            residents.Add(resident);
        }

        resident.UpdateOwnerDetails(
            command.AlternatePhone, command.NationalIdNumber, command.PassportNumber, command.DateOfBirth,
            command.Gender, command.PresentAddress, command.PermanentAddress, command.FatherName, command.MotherName,
            command.MaritalStatus, command.Profession, command.Employer, command.OfficeAddress,
            command.EmergencyContactName, command.EmergencyContactPhone, command.BloodGroup, command.Religion,
            command.Nationality, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Owner resident profile saved for resident {ResidentId} on flat {FlatId}, tenant {TenantId}",
            resident.Id, flatId, tenantId);

        return resident.ToDto();
    }
}
