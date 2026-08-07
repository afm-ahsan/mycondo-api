using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Amenities.Commands.CreateFacility;

public sealed class CreateFacilityCommandHandler(
    IFacilityRepository facilities,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateFacilityCommandHandler> logger
) : IRequestHandler<CreateFacilityCommand, FacilityDto>
{
    public async ValueTask<FacilityDto> Handle(CreateFacilityCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(command.BuildingId);
        Building building = await buildings.GetByIdAsync(buildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), command.BuildingId);
        if (building.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Building), command.BuildingId);
        }

        FacilityType facilityType = Enum.Parse<FacilityType>(command.FacilityType);

        Facility facility = Facility.Create(
            tenantId, buildingId, command.Name, facilityType, command.Capacity, command.OperatingHoursStart,
            command.OperatingHoursEnd, command.RequiresApproval, command.BookingChargeAmount, command.DepositAmount,
            command.CancellationDeadlineHours, command.CancellationDeductionPercentage, command.GuestFeeAmount,
            command.MinimumAgeUnaccompanied, command.RequiresSafetyAcknowledgement, command.BlocksEntryIfAccountOverdue,
            clock.UtcNow);

        facilities.Add(facility);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Facility {FacilityId} ('{Name}') created for tenant {TenantId}", facility.Id, facility.Name, tenantId);

        return facility.ToDto();
    }
}
