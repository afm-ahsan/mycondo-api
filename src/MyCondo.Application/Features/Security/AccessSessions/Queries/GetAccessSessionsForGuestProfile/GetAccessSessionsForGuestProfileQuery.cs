using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForGuestProfile;

public sealed record GetAccessSessionsForGuestProfileQuery(
    Guid GuestProfileId,
    int Page,
    int PageSize
) : IRequest<PagedResult<AccessSessionDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "security.visitors";
}
