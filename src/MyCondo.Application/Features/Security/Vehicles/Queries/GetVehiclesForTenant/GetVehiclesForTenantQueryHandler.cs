using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Vehicles.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.Vehicles.Queries.GetVehiclesForTenant;

public sealed class GetVehiclesForTenantQueryHandler(
    IVehicleRepository vehicles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetVehiclesForTenantQuery, PagedResult<VehicleDto>>
{
    public async ValueTask<PagedResult<VehicleDto>> Handle(GetVehiclesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<Vehicle> result = await vehicles.SearchAsync(
            tenantId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<VehicleDto> items = result.Items
            .Select(v => new VehicleDto(
                v.Id.Value, v.RegistrationNumber, v.VehicleType.ToString(), v.Make, v.Model, v.Color,
                v.OwnershipCategory.ToString(), v.FlatId?.Value, v.IsBlocked, v.BlockReason))
            .ToList();

        return new PagedResult<VehicleDto>(items, result.Page, result.PageSize, result.Total);
    }
}
