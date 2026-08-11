using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Commands.DisableResident;

public sealed class DisableResidentCommandHandler(
    IResidentRepository residents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<DisableResidentCommandHandler> logger
) : IRequestHandler<DisableResidentCommand>
{
    public async ValueTask<Unit> Handle(DisableResidentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId residentId = new(command.ResidentId);
        Resident resident = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), command.ResidentId);

        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), command.ResidentId);
        }

        resident.Deactivate(clock.UtcNow, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Resident {ResidentId} disabled for tenant {TenantId}", residentId, tenantId);

        return Unit.Value;
    }
}
