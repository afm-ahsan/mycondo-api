using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Leasing.Commands.EndWorkerAssignment;

public sealed class EndWorkerAssignmentCommandHandler(
    IOccupancyRegistrationWorkerAssignmentRepository assignments,
    IDomesticWorkerProfileRepository workers,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<EndWorkerAssignmentCommandHandler> logger
) : IRequestHandler<EndWorkerAssignmentCommand, OccupancyRegistrationWorkerAssignmentDto>
{
    public async ValueTask<OccupancyRegistrationWorkerAssignmentDto> Handle(
        EndWorkerAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationWorkerAssignmentId id = new(command.OccupancyRegistrationWorkerAssignmentId);
        OccupancyRegistrationWorkerAssignment assignment = await assignments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistrationWorkerAssignment), command.OccupancyRegistrationWorkerAssignmentId);
        if (assignment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistrationWorkerAssignment), command.OccupancyRegistrationWorkerAssignmentId);
        }

        assignment.End(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        DomesticWorkerProfile worker = await workers.GetByIdAsync(assignment.DomesticWorkerProfileId, cancellationToken)
            ?? throw new NotFoundException(nameof(DomesticWorkerProfile), assignment.DomesticWorkerProfileId.Value);

        logger.LogInformation(
            "Worker assignment {OccupancyRegistrationWorkerAssignmentId} ended, tenant {TenantId}", id, tenantId);

        return assignment.ToDto(worker);
    }
}
