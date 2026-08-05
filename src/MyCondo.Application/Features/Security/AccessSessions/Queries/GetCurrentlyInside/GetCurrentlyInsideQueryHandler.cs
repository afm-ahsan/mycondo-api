using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.AccessSessions;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetCurrentlyInside;

public sealed class GetCurrentlyInsideQueryHandler(
    IAccessSessionRepository accessSessions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCurrentlyInsideQuery, PagedResult<AccessSessionDto>>
{
    public async ValueTask<PagedResult<AccessSessionDto>> Handle(GetCurrentlyInsideQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        AccessCategory? category = query.Category is null ? null : Enum.Parse<AccessCategory>(query.Category);

        PagedResult<AccessSession> result = await accessSessions.SearchCurrentlyInsideAsync(
            tenantId, category, query.Page, query.PageSize, cancellationToken);

        List<AccessSessionDto> items = result.Items.Select(s => s.ToDto()).ToList();

        return new PagedResult<AccessSessionDto>(items, result.Page, result.PageSize, result.Total);
    }
}
