using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatById;

public sealed class GetFlatByIdQueryHandler(
    IFlatRepository flats,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatByIdQuery, FlatDto>
{
    public async ValueTask<FlatDto> Handle(GetFlatByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId id = new(query.FlatId);
        Flat flat = await flats.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), query.FlatId);

        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), query.FlatId);
        }

        return new FlatDto(flat.Id.Value, flat.BuildingId.Value, flat.FlatNumber, flat.FloorNumber, flat.FlatType.ToString(), flat.AreaSqFt);
    }
}
