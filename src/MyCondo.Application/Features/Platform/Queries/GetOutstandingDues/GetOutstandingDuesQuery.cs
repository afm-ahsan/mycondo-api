using Mediator;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Platform.Queries.GetOutstandingDues;

/// <summary>
/// Platform-scope collections view (ADR-034 Task 14D): current outstanding balance per organization,
/// grouped by currency, computed from persisted <c>SubscriptionInvoice.OutstandingAmount</c> —
/// never recalculated from package/subscription pricing. <paramref name="OrganizationId"/> narrows to
/// one organization's outstanding rows (one per currency it is billed in); omitted, every organization
/// with a collectible balance is returned, paginated.
/// </summary>
public sealed record GetOutstandingDuesQuery(
    Guid? OrganizationId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<PlatformOrganizationOutstandingDto>>;
