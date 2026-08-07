using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Commands.CreateBlackoutDate;

public sealed class CreateBlackoutDateCommandHandler(
    IFacilityRepository facilities,
    IBlackoutDateRepository blackoutDates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateBlackoutDateCommandHandler> logger
) : IRequestHandler<CreateBlackoutDateCommand, BlackoutDateDto>
{
    public async ValueTask<BlackoutDateDto> Handle(CreateBlackoutDateCommand command, CancellationToken cancellationToken)
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

        BlackoutDate blackoutDate = BlackoutDate.Create(
            tenantId, facilityId, command.DateFrom, command.DateTo, command.Reason, clock.UtcNow);

        blackoutDates.Add(blackoutDate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Blackout date {BlackoutDateId} created for facility {FacilityId}, tenant {TenantId}",
            blackoutDate.Id, facilityId, tenantId);

        return blackoutDate.ToDto();
    }
}
