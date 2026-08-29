using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Billing.Queries.GetFlatsMissingArea;

public sealed record GetFlatsMissingAreaQuery(
    Guid BuildingId,
    int Page,
    int PageSize
) : IRequest<PagedResult<FlatMissingAreaDto>>, ILifecycleReadOperation;
