using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.DeactivateServiceProviderAssignment;

public sealed class DeactivateServiceProviderAssignmentCommandHandler(
    IServiceProviderAssignmentRepository assignments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateServiceProviderAssignmentCommandHandler> logger
) : IRequestHandler<DeactivateServiceProviderAssignmentCommand>
{
    public async ValueTask<Unit> Handle(DeactivateServiceProviderAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ServiceProviderAssignmentId id = new(command.ServiceProviderAssignmentId);
        ServiceProviderAssignment assignment = await assignments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceProviderAssignment), command.ServiceProviderAssignmentId);

        if (assignment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ServiceProviderAssignment), command.ServiceProviderAssignmentId);
        }

        assignment.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service provider assignment {AssignmentId} deactivated for tenant {TenantId}", id, tenantId);

        return Unit.Value;
    }
}
