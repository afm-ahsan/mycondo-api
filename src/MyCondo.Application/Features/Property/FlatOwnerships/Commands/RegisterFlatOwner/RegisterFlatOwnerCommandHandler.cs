using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.RegisterFlatOwner;

public sealed class RegisterFlatOwnerCommandHandler(
    IResidentRepository residents,
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RegisterFlatOwnerCommandHandler> logger
) : IRequestHandler<RegisterFlatOwnerCommand, RegisterFlatOwnerResult>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<RegisterFlatOwnerResult> Handle(RegisterFlatOwnerCommand command, CancellationToken cancellationToken)
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

        // Reuse an existing party record for this Flat instead of creating a duplicate, matching the
        // convention Tenant Registration already relies on (Resident.FindByFlatAndNameAsync).
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
            command.EmergencyContactName, command.EmergencyContactPhone, clock.UtcNow);

        bool alreadyActive = await flatOwnerships.ExistsActiveForResidentAndFlatAsync(
            tenantId, resident.Id.Value, flatId, cancellationToken);
        if (alreadyActive)
        {
            throw new ConflictException("This resident already has an active ownership relationship with this flat.");
        }

        FlatOwnership ownership = FlatOwnership.Grant(tenantId, resident.Id.Value, flatId, command.StartDate, clock.UtcNow);
        flatOwnerships.Add(ownership);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Flat owner registered: resident {ResidentId} '{FullName}' granted ownership {FlatOwnershipId} of flat {FlatId}, tenant {TenantId}",
            resident.Id, resident.FullName, ownership.Id, flatId, tenantId);

        return new RegisterFlatOwnerResult(resident.Id.Value, ownership.Id.Value, resident.ToDto());
    }
}
