using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Gates.DTOs;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Application.Features.Property.Gates.Queries.GetGatesForBuilding;

public sealed class GetGatesForBuildingQueryHandler(
    IGateRepository gates,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGatesForBuildingQuery, List<GateDto>>
{
    public async ValueTask<List<GateDto>> Handle(GetGatesForBuildingQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(query.BuildingId);

        List<Gate> buildingGates = await gates.GetAllForBuildingAsync(
            tenantId, buildingId, query.ActiveOnly, cancellationToken);

        return buildingGates
            .Select(g => new GateDto(
                g.Id.Value, g.BuildingId.Value, g.Name, g.Code, g.Description, g.IsActive, g.IsEntryAllowed,
                g.IsExitAllowed, g.DisplayOrder))
            .ToList();
    }
}
