using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForFlat;

public sealed class GetFlatOwnershipsForFlatQueryHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IResidentRepository residents,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatOwnershipsForFlatQuery, List<FlatOwnershipDto>>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<List<FlatOwnershipDto>> Handle(GetFlatOwnershipsForFlatQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(query.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), query.FlatId);

        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), query.FlatId);
        }

        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        List<FlatOwnership> ownerships = await flatOwnerships.GetForFlatAsync(tenantId, flatId, cancellationToken);

        List<FlatOwnershipDto> items = [];
        foreach (FlatOwnership ownership in ownerships)
        {
            Resident? owner = await residents.GetByIdAsync(new ResidentId(ownership.ResidentId), cancellationToken);
            items.Add(new FlatOwnershipDto(
                ownership.Id.Value, ownership.ResidentId, owner?.FullName ?? "Unknown", ownership.FlatId.Value,
                ownership.Status.ToString(), ownership.StartDate, ownership.EndDate));
        }

        return items;
    }
}
