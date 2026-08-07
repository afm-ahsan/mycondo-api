using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Commands.UpdateFacilityConfiguration;

public sealed class UpdateFacilityConfigurationCommandHandler(
    IFacilityRepository facilities,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateFacilityConfigurationCommandHandler> logger
) : IRequestHandler<UpdateFacilityConfigurationCommand, FacilityDto>
{
    public async ValueTask<FacilityDto> Handle(UpdateFacilityConfigurationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId facilityId = new(command.FacilityId);
        Facility facility = await facilities.GetByIdAsync(facilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), command.FacilityId);
        if (facility.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Facility), command.FacilityId);
        }

        facility.UpdateConfiguration(
            command.Name, command.Capacity, command.OperatingHoursStart, command.OperatingHoursEnd,
            command.RequiresApproval, command.BookingChargeAmount, command.DepositAmount,
            command.CancellationDeadlineHours, command.CancellationDeductionPercentage, command.GuestFeeAmount,
            command.MinimumAgeUnaccompanied, command.RequiresSafetyAcknowledgement, command.BlocksEntryIfAccountOverdue);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Facility {FacilityId} configuration updated for tenant {TenantId}", facilityId, tenantId);

        return facility.ToDto();
    }
}
