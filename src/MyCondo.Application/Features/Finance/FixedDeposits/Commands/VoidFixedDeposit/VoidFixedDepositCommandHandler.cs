using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.FixedDeposits.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.VoidFixedDeposit;

/// <summary>
/// Voids a Fixed Deposit placed in error — only while still <see cref="FixedDepositStatus.Active"/> and
/// only when no interest has ever been accrued/received against it (checked here, not in the domain,
/// since it spans the separate accrual/receipt aggregates — see <see cref="FixedDeposit.Void"/>'s doc
/// comment). An FD with interest history is corrected by withdrawing it
/// (<c>WithdrawFixedDepositCommand</c>), never by voiding, so the interest history is never orphaned.
/// Posts the reversing "Dr CashOrBank / Cr FixedDeposit" entry — the placement's mirror image — through
/// <see cref="IFinancialPostingService"/>, never mutating the original placement posting.
/// </summary>
public sealed class VoidFixedDepositCommandHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinancialAccountRepository financialAccounts,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<VoidFixedDepositCommandHandler> logger
) : IRequestHandler<VoidFixedDepositCommand, FixedDepositDto>
{
    public async ValueTask<FixedDepositDto> Handle(VoidFixedDepositCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FixedDepositId fixedDepositId = new(command.FixedDepositId);
        FixedDeposit fixedDeposit = await fixedDeposits.GetByIdAsync(fixedDepositId, cancellationToken)
            ?? throw new NotFoundException(nameof(FixedDeposit), command.FixedDepositId);
        if (fixedDeposit.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FixedDeposit), command.FixedDepositId);
        }

        if (fixedDeposit.Status != FixedDepositStatus.Active)
        {
            throw new ConflictException($"Fixed Deposit {fixedDeposit.Id} is {fixedDeposit.Status} and cannot be voided.");
        }

        List<FixedDepositInterestAccrual> existingAccruals = await accruals.GetForFixedDepositAsync(fixedDepositId, cancellationToken);
        List<FixedDepositInterestReceipt> existingReceipts = await receipts.GetForFixedDepositAsync(fixedDepositId, cancellationToken);
        if (existingAccruals.Count > 0 || existingReceipts.Count > 0)
        {
            throw new ConflictException(
                $"Fixed Deposit {fixedDeposit.Id} has interest history and cannot be voided — withdraw it instead.");
        }

        FinancialAccount fundingAccount = await financialAccounts.GetByIdAsync(fixedDeposit.FundingFinancialAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), fixedDeposit.FundingFinancialAccountId.Value);

        DateOnly accountingDate = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(
                LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, fixedDeposit.Principal,
                ExplicitAccountId: fundingAccount.ChartOfAccountId),
            new FinancialPostingLine(LedgerAccountType.FixedDeposit, null, LedgerDirection.Credit, fixedDeposit.Principal),
        ];

        FinancialPostingResult reversal = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, accountingDate, $"Reversal of Fixed Deposit placement: {command.Reason}",
                "FixedDepositVoid", fixedDepositId.Value, postingLines, fixedDeposit.FundId),
            cancellationToken);

        fixedDeposit.Void(command.Reason, reversal.Posting.Id, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fixed Deposit {FixedDepositId} voided for tenant {TenantId}, reversal posting {PostingId}",
            fixedDepositId, tenantId, reversal.Posting.Id);

        return fixedDeposit.ToDto(fundingAccount.Name, null, null, 0m, 0m, accountingDate);
    }
}
