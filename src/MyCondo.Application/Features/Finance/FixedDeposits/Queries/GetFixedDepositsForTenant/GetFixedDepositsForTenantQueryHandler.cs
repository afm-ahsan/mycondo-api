using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.FixedDeposits.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositsForTenant;

public sealed class GetFixedDepositsForTenantQueryHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinancialAccountRepository financialAccounts,
    IFundRepository funds,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFixedDepositsForTenantQuery, PagedResult<FixedDepositDto>>
{
    public async ValueTask<PagedResult<FixedDepositDto>> Handle(
        GetFixedDepositsForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FixedDepositStatus? status = query.Status is string s ? Enum.Parse<FixedDepositStatus>(s) : null;
        FundId? fundId = query.FundId is Guid fundGuid ? new FundId(fundGuid) : null;
        FinancialAccountId? fundingAccountId = query.FundingFinancialAccountId is Guid accountGuid
            ? new FinancialAccountId(accountGuid)
            : null;

        PagedResult<FixedDeposit> page = await fixedDeposits.SearchAsync(
            tenantId, status, fundId, fundingAccountId, query.Page, query.PageSize, cancellationToken);

        List<FinancialAccount> allAccounts = await financialAccounts.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<FinancialAccountId, string> accountNamesById = allAccounts.ToDictionary(a => a.Id, a => a.Name);
        IReadOnlyList<Fund> allFunds = await funds.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<FundId, string> fundNamesById = allFunds.ToDictionary(f => f.Id, f => f.Name);
        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        List<FixedDepositDto> items = [];
        foreach (FixedDeposit fixedDeposit in page.Items)
        {
            decimal totalAccrued = await accruals.GetTotalAccruedAsync(fixedDeposit.Id, cancellationToken);
            decimal totalReceived = await receipts.GetTotalReceivedGrossAsync(fixedDeposit.Id, cancellationToken);

            items.Add(fixedDeposit.ToDto(
                accountNamesById.GetValueOrDefault(fixedDeposit.FundingFinancialAccountId),
                fixedDeposit.ReceivingFinancialAccountId is FinancialAccountId receivingId
                    ? accountNamesById.GetValueOrDefault(receivingId)
                    : null,
                fixedDeposit.FundId is FundId fdFundId ? fundNamesById.GetValueOrDefault(fdFundId) : null,
                totalAccrued, totalReceived, today));
        }

        return new PagedResult<FixedDepositDto>(items, page.Page, page.PageSize, page.Total);
    }
}
