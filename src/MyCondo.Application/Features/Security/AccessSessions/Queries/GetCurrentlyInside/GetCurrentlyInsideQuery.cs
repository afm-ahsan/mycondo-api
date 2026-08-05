using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetCurrentlyInside;

public sealed record GetCurrentlyInsideQuery(
    string? Category,
    int Page,
    int PageSize
) : IRequest<PagedResult<AccessSessionDto>>;
