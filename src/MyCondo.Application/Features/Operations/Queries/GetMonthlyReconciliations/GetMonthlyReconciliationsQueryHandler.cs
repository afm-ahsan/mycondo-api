using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

namespace MyCondo.Application.Features.Operations.Queries.GetMonthlyReconciliations;

public sealed class GetMonthlyReconciliationsQueryHandler(
    IMonthlyCylinderReconciliationRepository reconciliations,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMonthlyReconciliationsQuery, PagedResult<MonthlyCylinderReconciliationDto>>
{
    public async ValueTask<PagedResult<MonthlyCylinderReconciliationDto>> Handle(
        GetMonthlyReconciliationsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<MonthlyCylinderReconciliation> result = await reconciliations.SearchAsync(
            tenantId, query.CylinderType, query.Page, query.PageSize, cancellationToken);

        List<MonthlyCylinderReconciliationDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<MonthlyCylinderReconciliationDto>(items, result.Page, result.PageSize, result.Total);
    }
}
