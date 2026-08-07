using Mediator;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetMonthlyReconciliations;

public sealed record GetMonthlyReconciliationsQuery(
    string? CylinderType,
    int Page,
    int PageSize
) : IRequest<PagedResult<MonthlyCylinderReconciliationDto>>;
