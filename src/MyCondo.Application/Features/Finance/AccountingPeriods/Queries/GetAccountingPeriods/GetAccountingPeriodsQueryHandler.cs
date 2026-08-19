using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Queries.GetAccountingPeriods;

public sealed class GetAccountingPeriodsQueryHandler(
    IAccountingPeriodRepository accountingPeriods,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAccountingPeriodsQuery, List<AccountingPeriodDto>>
{
    public async ValueTask<List<AccountingPeriodDto>> Handle(GetAccountingPeriodsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<AccountingPeriod> periods = await accountingPeriods.GetAllForFinancialYearAsync(
            new FinancialYearId(query.FinancialYearId), cancellationToken);

        return periods
            .Select(p => new AccountingPeriodDto(p.Id.Value, p.FinancialYearId.Value, p.Name, p.StartDate, p.EndDate, p.Status.ToString()))
            .ToList();
    }
}
