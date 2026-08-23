using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Gates.DTOs;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Application.Features.Property.Gates.Queries.GetGatesForTenant;

public sealed class GetGatesForTenantQueryHandler(
    IGateRepository gates,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGatesForTenantQuery, List<GateDto>>
{
    public async ValueTask<List<GateDto>> Handle(GetGatesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;

        List<Gate> tenantGates = await gates.GetAllForTenantAsync(
            tenantId, buildingId, query.ActiveOnly, cancellationToken);

        return tenantGates
            .Select(g => new GateDto(
                g.Id.Value, g.BuildingId.Value, g.Name, g.Code, g.Description, g.IsActive, g.IsEntryAllowed,
                g.IsExitAllowed, g.DisplayOrder))
            .ToList();
    }
}
