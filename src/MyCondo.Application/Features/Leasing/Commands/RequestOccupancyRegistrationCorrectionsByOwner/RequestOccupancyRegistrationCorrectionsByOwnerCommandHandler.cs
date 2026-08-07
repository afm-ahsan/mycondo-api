using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

namespace MyCondo.Application.Features.Leasing.Commands.RequestOccupancyRegistrationCorrectionsByOwner;

public sealed class RequestOccupancyRegistrationCorrectionsByOwnerCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IOccupancyRegistrationStatusHistoryRepository history,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RequestOccupancyRegistrationCorrectionsByOwnerCommandHandler> logger
) : IRequestHandler<RequestOccupancyRegistrationCorrectionsByOwnerCommand, OccupancyRegistrationDto>
{
    public async ValueTask<OccupancyRegistrationDto> Handle(
        RequestOccupancyRegistrationCorrectionsByOwnerCommand command, CancellationToken cancellationToken)
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

        OccupancyRegistrationStatus fromStatus = registration.Status;
        registration.RequestCorrections(command.Reason, clock.UtcNow);

        history.Add(OccupancyRegistrationStatusHistory.Record(
            tenantId, id, fromStatus, registration.Status, currentUser.UserId, command.Reason, clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Occupancy registration {OccupancyRegistrationId} sent back for corrections by owner, tenant {TenantId}", id, tenantId);

        return registration.ToDto();
    }
}
