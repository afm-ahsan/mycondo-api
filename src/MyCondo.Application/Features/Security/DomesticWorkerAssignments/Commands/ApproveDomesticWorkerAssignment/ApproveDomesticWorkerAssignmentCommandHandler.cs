using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.ApproveDomesticWorkerAssignment;

public sealed class ApproveDomesticWorkerAssignmentCommandHandler(
    IDomesticWorkerAssignmentRepository assignments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ApproveDomesticWorkerAssignmentCommandHandler> logger
) : IRequestHandler<ApproveDomesticWorkerAssignmentCommand>
{
    public async ValueTask<Unit> Handle(ApproveDomesticWorkerAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DomesticWorkerAssignmentId id = new(command.DomesticWorkerAssignmentId);
        DomesticWorkerAssignment assignment = await assignments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(DomesticWorkerAssignment), command.DomesticWorkerAssignmentId);

        if (assignment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(DomesticWorkerAssignment), command.DomesticWorkerAssignmentId);
        }

        assignment.ApproveByResident();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Domestic worker assignment {AssignmentId} approved by resident for tenant {TenantId}", id, tenantId);

        return Unit.Value;
    }
}
