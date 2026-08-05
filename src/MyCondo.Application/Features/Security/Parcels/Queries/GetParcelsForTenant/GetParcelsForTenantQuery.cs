using Mediator;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.Parcels.Queries.GetParcelsForTenant;

public sealed record GetParcelsForTenantQuery(
    string? Status,
    Guid? RecipientFlatId,
    int Page,
    int PageSize
) : IRequest<PagedResult<ParcelDto>>;
