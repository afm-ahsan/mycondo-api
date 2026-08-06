using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadings;

public sealed record GetReadingsQuery(
    Guid? MeterId,
    Guid? FlatId,
    string? Status,
    int Page,
    int PageSize
) : IRequest<PagedResult<ReadingDto>>;
