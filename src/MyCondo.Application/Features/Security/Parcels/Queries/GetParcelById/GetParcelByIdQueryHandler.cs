using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Application.Features.Security.Parcels.Mappings;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Queries.GetParcelById;

public sealed class GetParcelByIdQueryHandler(
    IParcelRepository parcels,
    IFlatDisplayNameResolver flatDisplayNames,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetParcelByIdQuery, ParcelDto>
{
    public async ValueTask<ParcelDto> Handle(GetParcelByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ParcelId id = new(query.ParcelId);
        Parcel parcel = await parcels.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Parcel), query.ParcelId);

        if (parcel.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Parcel), query.ParcelId);
        }

        string recipientFlatDisplayName = await flatDisplayNames.ResolveAsync(parcel.RecipientFlatId, cancellationToken);
        return parcel.ToDto(recipientFlatDisplayName);
    }
}
