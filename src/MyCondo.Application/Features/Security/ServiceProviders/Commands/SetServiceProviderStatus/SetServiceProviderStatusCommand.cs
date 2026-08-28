using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.SetServiceProviderStatus;

public sealed record SetServiceProviderStatusCommand(
    Guid ServiceProviderProfileId,
    string Status,
    string? Reason
) : IRequest<ServiceProviderProfileDto>, IRequiresFeature
{
    public string FeatureKey => "security.service_providers";
}
