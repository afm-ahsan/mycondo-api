using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.RegisterServiceProvider;

public sealed record RegisterServiceProviderCommand(
    string FullName,
    string Phone,
    string ProviderType,
    string? ServiceDescription,
    string? IdentityDocumentType,
    string? IdentityDocumentNumber
) : IRequest<ServiceProviderProfileDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.service_providers";
}
