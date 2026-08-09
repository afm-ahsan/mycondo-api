using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.FlatOwnerships;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.EndFlatOwnership;

public sealed class EndFlatOwnershipCommandHandler(
    IFlatOwnershipRepository flatOwnerships,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<EndFlatOwnershipCommandHandler> logger
) : IRequestHandler<EndFlatOwnershipCommand>
{
    public async ValueTask<Unit> Handle(EndFlatOwnershipCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatOwnershipId id = new(command.FlatOwnershipId);
        FlatOwnership ownership = await flatOwnerships.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(FlatOwnership), command.FlatOwnershipId);

        if (ownership.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FlatOwnership), command.FlatOwnershipId);
        }

        ownership.End(command.EndDate, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FlatOwnership {FlatOwnershipId} ended for tenant {TenantId}", ownership.Id, tenantId);

        return Unit.Value;
    }
}
