using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.DeactivateServiceProviderAssignment;

public sealed record DeactivateServiceProviderAssignmentCommand(Guid ServiceProviderAssignmentId) : IRequest, IRequiresFeature
{
    public string FeatureKey => "security.service_providers";
}
