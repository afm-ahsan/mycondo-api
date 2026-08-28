using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.ApproveServiceProviderAssignment;

public sealed record ApproveServiceProviderAssignmentCommand(Guid ServiceProviderAssignmentId) : IRequest, IRequiresFeature
{
    public string FeatureKey => "security.service_providers";
}
