using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.Mappings;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Queries.GetAssignmentsForServiceProvider;

public sealed class GetAssignmentsForServiceProviderQueryHandler(
    IServiceProviderAssignmentRepository assignments,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAssignmentsForServiceProviderQuery, List<ServiceProviderAssignmentDto>>
{
    public async ValueTask<List<ServiceProviderAssignmentDto>> Handle(GetAssignmentsForServiceProviderQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<ServiceProviderAssignment> providerAssignments = await assignments.GetForProviderAsync(
            tenantId, new ServiceProviderProfileId(query.ServiceProviderProfileId), cancellationToken);

        return providerAssignments.Select(a => a.ToDto()).ToList();
    }
}
