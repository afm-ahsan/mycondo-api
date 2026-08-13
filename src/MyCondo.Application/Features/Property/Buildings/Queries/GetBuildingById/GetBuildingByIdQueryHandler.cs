using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;

public sealed class GetBuildingByIdQueryHandler(
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBuildingByIdQuery, BuildingDto>
{
    public async ValueTask<BuildingDto> Handle(GetBuildingByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId id = new(query.BuildingId);
        Building building = await buildings.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), query.BuildingId);

        if (building.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Building), query.BuildingId);
        }

        return new BuildingDto(building.Id.Value, building.Name, building.Code, building.Address, building.PrimaryPhotoAttachmentId);
    }
}
