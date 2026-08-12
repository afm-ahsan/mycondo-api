using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Commands.DeactivateFlat;

public sealed class DeactivateFlatCommandHandler(
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<DeactivateFlatCommandHandler> logger
) : IRequestHandler<DeactivateFlatCommand>
{
    public async ValueTask<Unit> Handle(DeactivateFlatCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        flat.Deactivate(clock.UtcNow, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Flat {FlatId} deactivated for tenant {TenantId}", flatId, tenantId);

        return Unit.Value;
    }
}
