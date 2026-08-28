using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Queries.GetAssignmentsForServiceProvider;

public sealed record GetAssignmentsForServiceProviderQuery(Guid ServiceProviderProfileId) : IRequest<List<ServiceProviderAssignmentDto>>, IRequiresFeature
{
    public string FeatureKey => "security.service_providers";
}
