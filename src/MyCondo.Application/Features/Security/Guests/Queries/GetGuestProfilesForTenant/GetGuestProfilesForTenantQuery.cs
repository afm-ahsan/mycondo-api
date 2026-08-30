using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.Guests.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.Guests.Queries.GetGuestProfilesForTenant;

public sealed record GetGuestProfilesForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<GuestProfileDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "security.visitors";
}
