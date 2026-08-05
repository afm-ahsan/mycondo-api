using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.DTOs;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.Mappings;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Queries.GetAssignmentsForDomesticWorker;

public sealed class GetAssignmentsForDomesticWorkerQueryHandler(
    IDomesticWorkerAssignmentRepository assignments,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAssignmentsForDomesticWorkerQuery, List<DomesticWorkerAssignmentDto>>
{
    public async ValueTask<List<DomesticWorkerAssignmentDto>> Handle(GetAssignmentsForDomesticWorkerQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<DomesticWorkerAssignment> workerAssignments = await assignments.GetForWorkerAsync(
            tenantId, new DomesticWorkerProfileId(query.DomesticWorkerProfileId), cancellationToken);

        return workerAssignments.Select(a => a.ToDto()).ToList();
    }
}
