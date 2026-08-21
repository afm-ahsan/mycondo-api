using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Application.Features.Finance.FinancialYears.Queries.GetFinancialYears;

public sealed class GetFinancialYearsQueryHandler(
    IFinancialYearRepository financialYears,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFinancialYearsQuery, List<FinancialYearDto>>
{
    public async ValueTask<List<FinancialYearDto>> Handle(GetFinancialYearsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<FinancialYear> years = await financialYears.GetAllForTenantAsync(tenantId, cancellationToken);

        return years
            .Select(y => new FinancialYearDto(y.Id.Value, y.Name, y.StartDate, y.EndDate, y.Status.ToString()))
            .ToList();
    }
}
