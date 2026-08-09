using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Me.Queries.GetMyFlats;

/// <summary>
/// Self-service: returns only the caller's own active ownership/occupancy relationships — never
/// accepts a UserId/FlatId parameter, so there is no way to ask about anyone else's Flats. Every
/// relationship IFlatAccessAuthorizer reports is still re-checked against the caller's own
/// building-scoped ownership.view/lease.view permission before being included — a relationship record
/// alone is never sufficient (see the Phase-3 "Role + Relationship Defense in Depth" invariant).
/// </summary>
public sealed class GetMyFlatsQueryHandler(
    IFlatAccessAuthorizer flatAccessAuthorizer,
    IFlatRepository flats,
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMyFlatsQuery, List<MyFlatDto>>
{
    private const string OwnershipViewPermission = "ownership.view";
    private const string LeaseViewPermission = "lease.view";

    public async ValueTask<List<MyFlatDto>> Handle(GetMyFlatsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId || currentUser.UserId is not Guid userId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<FlatRelationship> relationships =
            await flatAccessAuthorizer.GetActiveRelationshipsAsync(tenantId, userId, cancellationToken);

        List<MyFlatDto> result = [];

        foreach (FlatRelationship relationship in relationships)
        {
            string requiredPermission = relationship.Kind == FlatRelationshipKind.Ownership
                ? OwnershipViewPermission
                : LeaseViewPermission;

            if (!currentUser.HasPermissionForBuilding(requiredPermission, relationship.BuildingId))
            {
                continue;
            }

            Flat? flat = await flats.GetByIdAsync(new FlatId(relationship.FlatId), cancellationToken);
            Building? building = await buildings.GetByIdAsync(new BuildingId(relationship.BuildingId), cancellationToken);
            if (flat is null || building is null)
            {
                continue;
            }

            result.Add(new MyFlatDto(
                relationship.FlatId,
                flat.FlatNumber,
                relationship.BuildingId,
                building.Name,
                relationship.Kind.ToString(),
                relationship.StartDate,
                relationship.EndDate));
        }

        return result;
    }
}
