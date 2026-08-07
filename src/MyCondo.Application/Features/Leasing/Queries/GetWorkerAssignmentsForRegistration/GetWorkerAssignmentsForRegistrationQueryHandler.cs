using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Leasing.Queries.GetWorkerAssignmentsForRegistration;

public sealed class GetWorkerAssignmentsForRegistrationQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IOccupancyRegistrationWorkerAssignmentRepository assignments,
    IDomesticWorkerProfileRepository workers,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetWorkerAssignmentsForRegistrationQuery, IReadOnlyList<OccupancyRegistrationWorkerAssignmentDto>>
{
    public async ValueTask<IReadOnlyList<OccupancyRegistrationWorkerAssignmentDto>> Handle(
        GetWorkerAssignmentsForRegistrationQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId registrationId = new(query.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        }

        IReadOnlyList<OccupancyRegistrationWorkerAssignment> result =
            await assignments.GetForRegistrationAsync(registrationId, cancellationToken);

        List<OccupancyRegistrationWorkerAssignmentDto> dtos = [];
        foreach (OccupancyRegistrationWorkerAssignment assignment in result)
        {
            DomesticWorkerProfile? worker = await workers.GetByIdAsync(assignment.DomesticWorkerProfileId, cancellationToken);
            if (worker is not null)
            {
                dtos.Add(assignment.ToDto(worker));
            }
        }

        return dtos;
    }
}
