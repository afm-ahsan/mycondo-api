using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Billing.Queries.GetServiceChargeRules;

public sealed class GetServiceChargeRulesQueryHandler(
    IServiceChargeRuleRepository rules,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetServiceChargeRulesQuery, PagedResult<ServiceChargeRuleDto>>
{
    public async ValueTask<PagedResult<ServiceChargeRuleDto>> Handle(
        GetServiceChargeRulesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(query.BuildingId);
        PagedResult<ServiceChargeRule> result = await rules.SearchAsync(
            tenantId, buildingId, query.Category, query.Page, query.PageSize, cancellationToken);

        List<ServiceChargeRuleDto> items = result.Items.Select(r => r.ToDto()).ToList();

        return new PagedResult<ServiceChargeRuleDto>(items, result.Page, result.PageSize, result.Total);
    }
}
