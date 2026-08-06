using Mediator;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Billing.Queries.GetServiceChargeRules;

public sealed record GetServiceChargeRulesQuery(
    Guid BuildingId,
    string? Category,
    int Page,
    int PageSize
) : IRequest<PagedResult<ServiceChargeRuleDto>>;
