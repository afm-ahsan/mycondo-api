using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Commands.ReactivateFacility;

public sealed class ReactivateFacilityCommandHandler(
    IFacilityRepository facilities,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ReactivateFacilityCommandHandler> logger
) : IRequestHandler<ReactivateFacilityCommand, FacilityDto>
{
    public async ValueTask<FacilityDto> Handle(ReactivateFacilityCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId id = new(command.FacilityId);
        Facility facility = await facilities.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), command.FacilityId);
        if (facility.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Facility), command.FacilityId);
        }

        facility.Reactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Facility {FacilityId} reactivated, tenant {TenantId}", id, tenantId);

        return facility.ToDto();
    }
}
