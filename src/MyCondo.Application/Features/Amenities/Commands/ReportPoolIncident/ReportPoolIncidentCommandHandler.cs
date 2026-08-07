using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolIncidents;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Application.Features.Amenities.Commands.ReportPoolIncident;

public sealed class ReportPoolIncidentCommandHandler(
    IFacilityRepository facilities,
    IPoolIncidentRepository poolIncidents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ReportPoolIncidentCommandHandler> logger
) : IRequestHandler<ReportPoolIncidentCommand, PoolIncidentDto>
{
    public async ValueTask<PoolIncidentDto> Handle(ReportPoolIncidentCommand command, CancellationToken cancellationToken)
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

        PoolSessionId? poolSessionId = command.PoolSessionId is Guid rawSessionId ? new PoolSessionId(rawSessionId) : null;
        PoolIncidentSeverity severity = Enum.Parse<PoolIncidentSeverity>(command.Severity);

        PoolIncident incident = PoolIncident.Report(
            tenantId, facilityId, poolSessionId, command.OccurredAtUtc, currentUser.UserId, command.Description, severity,
            command.ActionTaken, clock.UtcNow);

        poolIncidents.Add(incident);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Pool incident {PoolIncidentId} reported at facility {FacilityId}, tenant {TenantId}", incident.Id, facilityId, tenantId);

        return incident.ToDto();
    }
}
