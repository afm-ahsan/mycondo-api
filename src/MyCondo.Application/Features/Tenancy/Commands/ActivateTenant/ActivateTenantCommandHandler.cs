using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Tenancy.Commands.ActivateTenant;

public sealed class ActivateTenantCommandHandler(
    ITenantRepository tenants,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ActivateTenantCommandHandler> logger
) : IRequestHandler<ActivateTenantCommand>
{
    public async ValueTask<Unit> Handle(ActivateTenantCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.TenantId);

        tenant.Activate(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tenant {TenantId} activated", tenant.Id);
        return Unit.Value;
    }
}
