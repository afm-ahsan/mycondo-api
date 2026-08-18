using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Application.Features.Security.Parcels.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Queries.GetParcelsForTenant;

public sealed class GetParcelsForTenantQueryHandler(
    IParcelRepository parcels,
    IFlatDisplayNameResolver flatDisplayNames,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetParcelsForTenantQuery, PagedResult<ParcelDto>>
{
    public async ValueTask<PagedResult<ParcelDto>> Handle(GetParcelsForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ParcelStatus? status = query.Status is null ? null : Enum.Parse<ParcelStatus>(query.Status);
        FlatId? flatId = query.RecipientFlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;

        PagedResult<Parcel> result = await parcels.SearchAsync(
            tenantId, status, flatId, query.Page, query.PageSize, cancellationToken);

        IReadOnlyDictionary<FlatId, string> flatDisplayNamesById = await flatDisplayNames.ResolveManyAsync(
            result.Items.Select(p => p.RecipientFlatId).Distinct().ToList(), cancellationToken);

        List<ParcelDto> items = result.Items
            .Select(p => p.ToDto(flatDisplayNamesById.GetValueOrDefault(p.RecipientFlatId, "Unknown flat")))
            .ToList();

        return new PagedResult<ParcelDto>(items, result.Page, result.PageSize, result.Total);
    }
}
