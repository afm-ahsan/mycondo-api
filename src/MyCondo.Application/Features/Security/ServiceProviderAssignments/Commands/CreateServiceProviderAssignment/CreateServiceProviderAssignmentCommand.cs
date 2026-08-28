using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.CreateServiceProviderAssignment;

public sealed record CreateServiceProviderAssignmentCommand(
    Guid ServiceProviderProfileId,
    Guid FlatId,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? AllowedDays,
    TimeOnly? AllowedStartTime,
    TimeOnly? AllowedEndTime
) : IRequest<ServiceProviderAssignmentDto>, IRequiresFeature
{
    public string FeatureKey => "security.service_providers";
}
