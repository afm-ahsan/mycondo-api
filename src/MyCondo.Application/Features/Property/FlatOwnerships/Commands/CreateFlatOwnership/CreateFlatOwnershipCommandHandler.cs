using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;

public sealed class CreateFlatOwnershipCommandHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IResidentRepository residents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateFlatOwnershipCommandHandler> logger
) : IRequestHandler<CreateFlatOwnershipCommand, CreateFlatOwnershipResult>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<CreateFlatOwnershipResult> Handle(CreateFlatOwnershipCommand command, CancellationToken cancellationToken)
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

        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        ResidentId residentId = new(command.ResidentId);
        Resident resident = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), command.ResidentId);

        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), command.ResidentId);
        }

        bool alreadyActive = await flatOwnerships.ExistsActiveForResidentAndFlatAsync(
            tenantId, command.ResidentId, flatId, cancellationToken);
        if (alreadyActive)
        {
            throw new ConflictException("This resident already has an active ownership relationship with this flat.");
        }

        FlatOwnership ownership = FlatOwnership.Grant(tenantId, command.ResidentId, flatId, command.StartDate, clock.UtcNow);

        flatOwnerships.Add(ownership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FlatOwnership {FlatOwnershipId} granted: resident {ResidentId} owns flat {FlatId} for tenant {TenantId}",
            ownership.Id, command.ResidentId, command.FlatId, tenantId);

        return new CreateFlatOwnershipResult(ownership.Id.Value, command.ResidentId, command.FlatId, command.StartDate);
    }
}
