using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.FixedDeposits.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositById;

public sealed class GetFixedDepositByIdQueryHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinancialAccountRepository financialAccounts,
    IFundRepository funds,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFixedDepositByIdQuery, FixedDepositDetailDto>
{
    public async ValueTask<FixedDepositDetailDto> Handle(GetFixedDepositByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FixedDepositId fixedDepositId = new(query.FixedDepositId);
        FixedDeposit fixedDeposit = await fixedDeposits.GetByIdAsync(fixedDepositId, cancellationToken)
            ?? throw new NotFoundException(nameof(FixedDeposit), query.FixedDepositId);
        if (fixedDeposit.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FixedDeposit), query.FixedDepositId);
        }

        FinancialAccount? fundingAccount = await financialAccounts.GetByIdAsync(fixedDeposit.FundingFinancialAccountId, cancellationToken);
        FinancialAccount? receivingAccount = fixedDeposit.ReceivingFinancialAccountId is FinancialAccountId receivingId
            ? await financialAccounts.GetByIdAsync(receivingId, cancellationToken)
            : null;
        Fund? fund = fixedDeposit.FundId is FundId fundId ? await funds.GetByIdAsync(fundId, cancellationToken) : null;

        List<FixedDepositInterestAccrual> accrualEntries = await accruals.GetForFixedDepositAsync(fixedDepositId, cancellationToken);
        List<FixedDepositInterestReceipt> receiptEntries = await receipts.GetForFixedDepositAsync(fixedDepositId, cancellationToken);
        decimal totalAccrued = accrualEntries.Where(a => !a.IsReversed).Sum(a => a.GrossAmount);
        decimal totalReceived = receiptEntries.Where(r => !r.IsReversed).Sum(r => r.GrossAmount);
        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        FixedDepositDto dto = fixedDeposit.ToDto(
            fundingAccount?.Name, receivingAccount?.Name, fund?.Name, totalAccrued, totalReceived, today);

        return new FixedDepositDetailDto(
            dto,
            accrualEntries.Select(a => a.ToDto()).ToList(),
            receiptEntries.Select(r => r.ToDto()).ToList());
    }
}
