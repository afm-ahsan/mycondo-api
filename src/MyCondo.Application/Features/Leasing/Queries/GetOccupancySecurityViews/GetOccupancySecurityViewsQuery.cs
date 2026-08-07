using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancySecurityViews;

/// <summary>Always restricted to Active occupancies server-side — security only needs to see who
/// currently has access, not the full application history.</summary>
public sealed record GetOccupancySecurityViewsQuery(
    Guid? FlatId, int Page, int PageSize
) : IRequest<PagedResult<OccupancyRegistrationSecuritySummaryDto>>;
