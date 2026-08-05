using Mediator;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.RegisterServiceProvider;

public sealed record RegisterServiceProviderCommand(
    string FullName,
    string Phone,
    string ProviderType,
    string? ServiceDescription,
    string? IdentityDocumentType,
    string? IdentityDocumentNumber
) : IRequest<ServiceProviderProfileDto>;
