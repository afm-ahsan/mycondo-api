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

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RenewFixedDeposit;

/// <summary>
/// Renews a Fixed Deposit — creates its successor (<see cref="FixedDeposit.PlaceAsRenewal"/>) and marks
/// the predecessor <see cref="FixedDepositStatus.Renewed"/>, preserving lineage rather than overwriting
/// (Template 4's "never overwrite a matured FD"). Posts the *difference* between the predecessor's and
/// the new principal, matching the DoD reconciliation's explicit "Capitalized Interest" movement type:
/// <list type="bullet">
/// <item>unchanged principal → no ledger movement, pure lineage continuation;</item>
/// <item>increased principal → "Dr FixedDeposit / Cr InterestReceivable" (accrued interest capitalized
/// into the new instrument — capped at the FD's outstanding receivable, same accrual-first rule
/// <c>RecordFixedDepositInterestReceiptCommandHandler</c> enforces);</item>
/// <item>decreased principal → "Dr CashOrBank / Cr FixedDeposit" (a partial withdrawal taken at renewal).
/// </item>
/// </list>
/// </summary>
public sealed class RenewFixedDepositCommandHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinancialAccountRepository financialAccounts,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RenewFixedDepositCommandHandler> logger
) : IRequestHandler<RenewFixedDepositCommand, FixedDepositDto>
{
    public async ValueTask<FixedDepositDto> Handle(RenewFixedDepositCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FixedDepositId predecessorId = new(command.FixedDepositId);
        FixedDeposit predecessor = await fixedDeposits.GetByIdAsync(predecessorId, cancellationToken)
            ?? throw new NotFoundException(nameof(FixedDeposit), command.FixedDepositId);
        if (predecessor.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FixedDeposit), command.FixedDepositId);
        }

        if (predecessor.Status != FixedDepositStatus.Active)
        {
            throw new ConflictException($"Fixed Deposit {predecessor.Id} is {predecessor.Status} and cannot be renewed.");
        }

        string newCertificateNumber = command.NewCertificateNumber.Trim();
        if (await fixedDeposits.ExistsForCertificateNumberAsync(tenantId, newCertificateNumber, cancellationToken))
        {
            throw new ConflictException($"A Fixed Deposit with certificate number '{newCertificateNumber}' already exists.");
        }

        FinancialAccountId fundingAccountId = new(command.FundingFinancialAccountId);
        FinancialAccount fundingAccount = await financialAccounts.GetByIdAsync(fundingAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), command.FundingFinancialAccountId);
        if (fundingAccount.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FinancialAccount), command.FundingFinancialAccountId);
        }

        if (!fundingAccount.IsActive)
        {
            throw new ConflictException($"Financial Account {fundingAccount.Id} is inactive and cannot be used for renewal.");
        }

        decimal principalDifference = command.NewPrincipal - predecessor.Principal;
        LedgerPostingId? renewalAdjustmentPostingId = null;

        if (principalDifference > 0)
        {
            decimal totalAccrued = await accruals.GetTotalAccruedAsync(predecessorId, cancellationToken);
            decimal totalReceived = await receipts.GetTotalReceivedGrossAsync(predecessorId, cancellationToken);
            decimal outstandingReceivable = totalAccrued - totalReceived;
            if (principalDifference > outstandingReceivable)
            {
                throw new ConflictException(
                    $"Fixed Deposit {predecessor.Id} has only {outstandingReceivable} of accrued interest outstanding to " +
                    $"capitalize; the renewal principal increase of {principalDifference} exceeds it.");
            }

            FinancialPostingLine[] capitalizationLines =
            [
                new FinancialPostingLine(LedgerAccountType.FixedDeposit, null, LedgerDirection.Debit, principalDifference),
                new FinancialPostingLine(LedgerAccountType.InterestReceivable, null, LedgerDirection.Credit, principalDifference),
            ];

            FinancialPostingResult capitalization = await financialPosting.PostAsync(
                new FinancialPostingRequest(
                    tenantId, command.NewStartDate, $"FD renewal interest capitalization: {predecessor.CertificateNumber} -> {newCertificateNumber}",
                    "FixedDepositRenewalCapitalization", null, capitalizationLines, predecessor.FundId),
                cancellationToken);
            renewalAdjustmentPostingId = capitalization.Posting.Id;
        }
        else if (principalDifference < 0)
        {
            decimal withdrawnAmount = -principalDifference;
            FinancialPostingLine[] withdrawalLines =
            [
                new FinancialPostingLine(
                    LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, withdrawnAmount,
                    ExplicitAccountId: fundingAccount.ChartOfAccountId),
                new FinancialPostingLine(LedgerAccountType.FixedDeposit, null, LedgerDirection.Credit, withdrawnAmount),
            ];

            FinancialPostingResult partialWithdrawal = await financialPosting.PostAsync(
                new FinancialPostingRequest(
                    tenantId, command.NewStartDate, $"FD renewal partial withdrawal: {predecessor.CertificateNumber} -> {newCertificateNumber}",
                    "FixedDepositRenewalPartialWithdrawal", null, withdrawalLines, predecessor.FundId),
                cancellationToken);
            renewalAdjustmentPostingId = partialWithdrawal.Posting.Id;
        }

        FixedDepositId successorId = FixedDepositId.New();
        FixedDeposit successor = FixedDeposit.PlaceAsRenewal(
            successorId, predecessor, newCertificateNumber, command.NewBranchName, fundingAccountId,
            command.NewPrincipal, command.NewInterestRatePercent,
            Enum.Parse<InterestCalculationMethod>(command.NewCalculationMethod),
            Enum.Parse<InterestPaymentFrequency>(command.NewPaymentFrequency), command.NewStartDate,
            command.NewMaturityDate, command.NewExpectedGrossInterest, command.NewExpectedDeductionRatePercent,
            command.Notes, renewalAdjustmentPostingId, clock.UtcNow);

        predecessor.MarkRenewed(successorId, renewalAdjustmentPostingId, clock.UtcNow);
        fixedDeposits.Add(successor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fixed Deposit {PredecessorId} renewed as {SuccessorId} '{CertificateNumber}' for tenant {TenantId}",
            predecessorId, successorId, newCertificateNumber, tenantId);

        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return successor.ToDto(fundingAccount.Name, null, null, 0m, 0m, today);
    }
}
