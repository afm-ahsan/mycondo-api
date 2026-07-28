using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Tenancy.Commands.SuspendTenant;

public sealed class SuspendTenantCommandHandler(
    ITenantRepository tenants,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<SuspendTenantCommandHandler> logger
) : IRequestHandler<SuspendTenantCommand>
{
    public async ValueTask<Unit> Handle(SuspendTenantCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.TenantId);

        tenant.Suspend(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tenant {TenantId} suspended", tenant.Id);
        return Unit.Value;
    }
}
