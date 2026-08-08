using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.DTOs;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Queries.GetSecuritySummaryReport;

public sealed class GetSecuritySummaryReportQueryHandler(
    IAccessSessionRepository accessSessions,
    IParcelRepository parcels,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetSecuritySummaryReportQuery, SecuritySummaryDto>
{
    public async ValueTask<SecuritySummaryDto> Handle(GetSecuritySummaryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<CurrentlyInsideCategoryCount> currentlyInside =
            await accessSessions.GetCurrentlyInsideCountsByCategoryAsync(tenantId, cancellationToken);
        int awaitingCollection = await parcels.GetAwaitingCollectionCountAsync(tenantId, cancellationToken);

        List<CurrentlyInsideCategoryCountDto> currentlyInsideDtos = currentlyInside
            .Select(c => new CurrentlyInsideCategoryCountDto(c.Category.ToString(), c.Count))
            .ToList();

        return new SecuritySummaryDto(currentlyInsideDtos, awaitingCollection);
    }
}
