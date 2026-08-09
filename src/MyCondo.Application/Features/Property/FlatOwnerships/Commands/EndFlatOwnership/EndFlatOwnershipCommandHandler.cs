using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.EndFlatOwnership;

public sealed class EndFlatOwnershipCommandHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<EndFlatOwnershipCommandHandler> logger
) : IRequestHandler<EndFlatOwnershipCommand>
{
    private const string OwnershipManagePermission = "ownership.manage";

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

        Flat flat = await flats.GetByIdAsync(ownership.FlatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), ownership.FlatId.Value);

        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        ownership.End(command.EndDate, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FlatOwnership {FlatOwnershipId} ended for tenant {TenantId}", ownership.Id, tenantId);

        return Unit.Value;
    }
}
