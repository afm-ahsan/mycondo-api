using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;
using MyCondo.Application.Features.Security.ServiceProviders.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.SetServiceProviderStatus;

public sealed class SetServiceProviderStatusCommandHandler(
    IServiceProviderProfileRepository profiles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetServiceProviderStatusCommandHandler> logger
) : IRequestHandler<SetServiceProviderStatusCommand, ServiceProviderProfileDto>
{
    public async ValueTask<ServiceProviderProfileDto> Handle(SetServiceProviderStatusCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ServiceProviderProfileId id = new(command.ServiceProviderProfileId);
        ServiceProviderProfile profile = await profiles.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceProviderProfile), command.ServiceProviderProfileId);

        if (profile.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ServiceProviderProfile), command.ServiceProviderProfileId);
        }

        RecurringAccessProfileStatus status = Enum.Parse<RecurringAccessProfileStatus>(command.Status);
        switch (status)
        {
            case RecurringAccessProfileStatus.Active:
                profile.Reactivate();
                break;
            case RecurringAccessProfileStatus.Suspended:
                profile.Suspend(command.Reason!);
                break;
            case RecurringAccessProfileStatus.Blocked:
                profile.Block(command.Reason!);
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service provider profile {ProfileId} status set to {Status} for tenant {TenantId}",
            id, status, tenantId);

        return profile.ToDto();
    }
}
