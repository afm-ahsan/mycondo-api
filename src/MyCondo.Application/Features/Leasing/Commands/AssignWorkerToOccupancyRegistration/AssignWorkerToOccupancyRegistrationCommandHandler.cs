using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Leasing.Commands.AssignWorkerToOccupancyRegistration;

/// <summary>
/// Links an existing <see cref="DomesticWorkerProfile"/> — created and searched for via the Security
/// module's own <c>POST/GET /api/v1/domestic-workers</c>, never duplicated here — to a Tenant
/// Registration. A worker with <see cref="DomesticWorkerType.Driver"/> is how "driver" is represented;
/// there is no separate driver concept in this codebase.
/// </summary>
public sealed class AssignWorkerToOccupancyRegistrationCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IOccupancyRegistrationWorkerAssignmentRepository assignments,
    IDomesticWorkerProfileRepository workers,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AssignWorkerToOccupancyRegistrationCommandHandler> logger
) : IRequestHandler<AssignWorkerToOccupancyRegistrationCommand, OccupancyRegistrationWorkerAssignmentDto>
{
    public async ValueTask<OccupancyRegistrationWorkerAssignmentDto> Handle(
        AssignWorkerToOccupancyRegistrationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId registrationId = new(command.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        }

        DomesticWorkerProfileId workerId = new(command.DomesticWorkerProfileId);
        DomesticWorkerProfile worker = await workers.GetByIdAsync(workerId, cancellationToken)
            ?? throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);
        if (worker.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);
        }

        OccupancyRegistrationWorkerAssignment assignment = OccupancyRegistrationWorkerAssignment.Assign(
            tenantId, registrationId, workerId, clock.UtcNow);

        assignments.Add(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Worker {DomesticWorkerProfileId} assigned to occupancy registration {OccupancyRegistrationId}, tenant {TenantId}",
            workerId, registrationId, tenantId);

        return assignment.ToDto(worker);
    }
}
