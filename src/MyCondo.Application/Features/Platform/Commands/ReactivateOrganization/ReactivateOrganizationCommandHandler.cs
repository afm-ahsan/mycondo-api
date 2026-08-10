using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ReactivateOrganization;

public sealed class ReactivateOrganizationCommandHandler(
    ITenantRepository tenants,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ReactivateOrganizationCommandHandler> logger
) : IRequestHandler<ReactivateOrganizationCommand>
{
    public async ValueTask<Unit> Handle(ReactivateOrganizationCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        tenant.Reactivate(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Organization {TenantId} reactivated", tenant.Id);
        return Unit.Value;
    }
}
