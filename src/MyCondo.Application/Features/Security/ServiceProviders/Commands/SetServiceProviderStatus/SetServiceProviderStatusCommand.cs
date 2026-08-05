using Mediator;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.SetServiceProviderStatus;

public sealed record SetServiceProviderStatusCommand(
    Guid ServiceProviderProfileId,
    string Status,
    string? Reason
) : IRequest<ServiceProviderProfileDto>;
