using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForFlat;

public sealed class GetFlatOwnershipsForFlatQueryHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatOwnershipsForFlatQuery, List<FlatOwnershipDto>>
{
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

        List<FlatOwnership> ownerships = await flatOwnerships.GetForFlatAsync(tenantId, flatId, cancellationToken);

        return ownerships
            .Select(o => new FlatOwnershipDto(o.Id.Value, o.UserId, o.FlatId.Value, o.Status.ToString(), o.StartDate, o.EndDate))
            .ToList();
    }
}
