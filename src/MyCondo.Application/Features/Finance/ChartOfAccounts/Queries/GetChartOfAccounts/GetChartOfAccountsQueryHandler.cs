using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.ChartOfAccounts.DTOs;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.ChartOfAccounts.Queries.GetChartOfAccounts;

public sealed class GetChartOfAccountsQueryHandler(
    IChartOfAccountRepository chartOfAccounts,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetChartOfAccountsQuery, List<ChartOfAccountDto>>
{
    public async ValueTask<List<ChartOfAccountDto>> Handle(GetChartOfAccountsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<ChartOfAccount> accounts = await chartOfAccounts.GetAllForTenantAsync(tenantId, cancellationToken);

        return accounts
            .Select(a => new ChartOfAccountDto(
                a.Id.Value, a.Code, a.Name, a.Category.ToString(), a.NormalBalance.ToString(),
                a.ParentAccountId?.Value, a.IsSystemAccount, a.IsActive))
            .ToList();
    }
}
