using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForOwner;

public sealed class GetFlatOwnershipsForOwnerQueryHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IBuildingRepository buildings,
    IResidentRepository residents,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatOwnershipsForOwnerQuery, List<OwnerFlatOwnershipDto>>
{
    public async ValueTask<List<OwnerFlatOwnershipDto>> Handle(
        GetFlatOwnershipsForOwnerQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId residentId = new(query.ResidentId);
        Resident owner = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), query.ResidentId);

        if (owner.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), query.ResidentId);
        }

        List<FlatOwnership> ownerships = await flatOwnerships.GetAllForResidentAsync(tenantId, query.ResidentId, cancellationToken);

        List<OwnerFlatOwnershipDto> items = [];
        foreach (FlatOwnership ownership in ownerships)
        {
            Flat? flat = await flats.GetByIdAsync(ownership.FlatId, cancellationToken);
            if (flat is null)
            {
                continue;
            }

            Building? building = await buildings.GetByIdAsync(flat.BuildingId, cancellationToken);

            items.Add(new OwnerFlatOwnershipDto(
                ownership.Id.Value,
                flat.Id.Value,
                flat.FlatNumber,
                flat.BuildingId.Value,
                building?.Name ?? "Unknown",
                ownership.Status.ToString(),
                ownership.StartDate,
                ownership.EndDate));
        }

        return items;
    }
}
