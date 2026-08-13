using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingsForTenant;

public sealed class GetBuildingsForTenantQueryHandler(
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBuildingsForTenantQuery, PagedResult<BuildingDto>>
{
    public async ValueTask<PagedResult<BuildingDto>> Handle(GetBuildingsForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<Building> result = await buildings.SearchAsync(
            tenantId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<BuildingDto> items = result.Items
            .Select(b => new BuildingDto(b.Id.Value, b.Name, b.Code, b.Address, b.PrimaryPhotoAttachmentId))
            .ToList();

        return new PagedResult<BuildingDto>(items, result.Page, result.PageSize, result.Total);
    }
}
