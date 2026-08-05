using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;
using MyCondo.Application.Features.Security.ServiceProviders.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.RegisterServiceProvider;

public sealed class RegisterServiceProviderCommandHandler(
    IServiceProviderProfileRepository profiles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RegisterServiceProviderCommandHandler> logger
) : IRequestHandler<RegisterServiceProviderCommand, ServiceProviderProfileDto>
{
    public async ValueTask<ServiceProviderProfileDto> Handle(RegisterServiceProviderCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ServiceProviderType providerType = Enum.Parse<ServiceProviderType>(command.ProviderType);

        ServiceProviderProfile profile = ServiceProviderProfile.Register(
            tenantId, command.FullName, command.Phone, providerType, command.ServiceDescription,
            command.IdentityDocumentType, command.IdentityDocumentNumber, clock.UtcNow);

        profiles.Add(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service provider profile {ProfileId} '{FullName}' registered for tenant {TenantId}",
            profile.Id, profile.FullName, tenantId);

        return profile.ToDto();
    }
}
