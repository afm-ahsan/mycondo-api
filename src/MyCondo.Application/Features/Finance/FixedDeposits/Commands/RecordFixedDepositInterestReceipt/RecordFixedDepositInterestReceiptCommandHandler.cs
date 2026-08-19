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

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestReceipt;

/// <summary>
/// Records an actual interest receipt against a Fixed Deposit's accrued Interest Receivable —
/// "Dr CashOrBank (net) [+ Dr InterestDeductionExpense (deduction)] / Cr InterestReceivable (gross)".
/// Enforces the accrual-first workflow: a receipt can never exceed the FD's outstanding accrued-but-
/// unreceived balance (Template 4's "Interest Earned != Interest Received" distinction) — record the
/// accrual first if it hasn't been for this period.
/// </summary>
public sealed class RecordFixedDepositInterestReceiptCommandHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinancialAccountRepository financialAccounts,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordFixedDepositInterestReceiptCommandHandler> logger
) : IRequestHandler<RecordFixedDepositInterestReceiptCommand, FixedDepositInterestReceiptDto>
{
    public async ValueTask<FixedDepositInterestReceiptDto> Handle(
        RecordFixedDepositInterestReceiptCommand command, CancellationToken cancellationToken)
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
            throw new ConflictException($"Fixed Deposit {fixedDeposit.Id} is {fixedDeposit.Status} and cannot receive interest.");
        }

        FinancialAccountId receivingAccountId = new(command.ReceivingFinancialAccountId);
        FinancialAccount receivingAccount = await financialAccounts.GetByIdAsync(receivingAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), command.ReceivingFinancialAccountId);
        if (receivingAccount.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FinancialAccount), command.ReceivingFinancialAccountId);
        }

        if (!receivingAccount.IsActive)
        {
            throw new ConflictException($"Financial Account {receivingAccount.Id} is inactive and cannot receive interest.");
        }

        decimal totalAccrued = await accruals.GetTotalAccruedAsync(fixedDepositId, cancellationToken);
        decimal totalReceived = await receipts.GetTotalReceivedGrossAsync(fixedDepositId, cancellationToken);
        decimal outstandingReceivable = totalAccrued - totalReceived;
        if (command.GrossAmount > outstandingReceivable)
        {
            throw new ConflictException(
                $"Fixed Deposit {fixedDeposit.Id} has only {outstandingReceivable} of accrued interest outstanding; " +
                $"record the interest accrual before receiving {command.GrossAmount}.");
        }

        DateOnly accountingDate = command.AccountingDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        decimal netAmount = command.GrossAmount - command.DeductionAmount;
        FixedDepositInterestReceiptId receiptId = FixedDepositInterestReceiptId.New();

        // netAmount can be zero (fully deducted at source) — a zero-amount ledger line is rejected by
        // LedgerPosting.Create, so it's only added when there's an actual cash movement to record.
        List<FinancialPostingLine> postingLines = [];
        if (netAmount > 0)
        {
            postingLines.Add(new FinancialPostingLine(
                LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, netAmount,
                ExplicitAccountId: receivingAccount.ChartOfAccountId));
        }

        if (command.DeductionAmount > 0)
        {
            postingLines.Add(new FinancialPostingLine(
                LedgerAccountType.InterestDeductionExpense, null, LedgerDirection.Debit, command.DeductionAmount));
        }

        postingLines.Add(new FinancialPostingLine(
            LedgerAccountType.InterestReceivable, null, LedgerDirection.Credit, command.GrossAmount));

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, accountingDate, $"FD interest receipt: {fixedDeposit.CertificateNumber}",
                "FixedDepositInterestReceipt", receiptId.Value, postingLines, fixedDeposit.FundId),
            cancellationToken);

        FixedDepositInterestReceipt receipt = FixedDepositInterestReceipt.Record(
            receiptId, tenantId, fixedDepositId, accountingDate, command.GrossAmount, command.DeductionAmount,
            receivingAccountId, command.ReferenceNumber, command.Notes, posted.Posting.Id, clock.UtcNow);

        receipts.Add(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FD Interest Receipt {ReceiptId} recorded for Fixed Deposit {FixedDepositId}, tenant {TenantId}, posting {PostingId}",
            receipt.Id, fixedDepositId, tenantId, posted.Posting.Id);

        return receipt.ToDto();
    }
}
