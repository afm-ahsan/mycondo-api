using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.FixedDeposits.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Audit;
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
///
/// Each renewal posting carries, as its <c>SourceId</c>, the Fixed Deposit whose own principal it moves —
/// the same convention <c>FixedDepositPlacement</c>/<c>FixedDepositMaturity</c>/<c>FixedDepositVoid</c>
/// already use, and the reason the tenant-wide <c>LedgerAccountType.FixedDeposit</c> account can be
/// decomposed per instrument at all (Phase 2A Task 5's Fixed Deposits supporting schedule attributes
/// ledger activity through <c>LedgerPosting.ReferenceId</c>; these two postings previously supplied none
/// and surfaced as unexplained reconciliation differences).
/// <list type="bullet">
/// <item>Capitalization establishes the <em>successor's</em> higher principal — it is the successor's own
/// placement-equivalent posting, which is exactly why <see cref="FixedDeposit.PlaceAsRenewal"/> already
/// records it as the successor's <c>PlacementPostingId</c> — so its source is the successor.</item>
/// <item>A partial withdrawal returns part of the <em>predecessor's</em> principal, the same movement
/// <c>FixedDepositMaturity</c> records against the deposit being withdrawn from, so its source is the
/// predecessor. Attributing it to the successor instead would give the live instrument a negative
/// carrying balance on the supporting schedule.</item>
/// </list>
/// Both are existing <see cref="FixedDepositId"/> values and both keys are unique for the posting
/// service's (tenant, purpose, source) idempotency guard, since an Active deposit can only be renewed
/// once and a successor id is freshly generated per renewal.
/// </summary>
public sealed class RenewFixedDepositCommandHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinancialAccountRepository financialAccounts,
    IFinancialPostingService financialPosting,
    IFinanceAuditLogRepository auditLog,
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

        // Generated before the postings so each one can carry the Fixed Deposit it moves principal for as
        // its SourceId — the same "post first, then construct the source record" ordering
        // PlaceFixedDepositCommandHandler already uses.
        FixedDepositId successorId = FixedDepositId.New();

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
                    "FixedDepositRenewalCapitalization", successorId.Value, capitalizationLines, predecessor.FundId),
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
                    "FixedDepositRenewalPartialWithdrawal", predecessorId.Value, withdrawalLines, predecessor.FundId),
                cancellationToken);
            renewalAdjustmentPostingId = partialWithdrawal.Posting.Id;
        }

        FixedDeposit successor = FixedDeposit.PlaceAsRenewal(
            successorId, predecessor, newCertificateNumber, command.NewBranchName, fundingAccountId,
            command.NewPrincipal, command.NewInterestRatePercent,
            Enum.Parse<InterestCalculationMethod>(command.NewCalculationMethod),
            Enum.Parse<InterestPaymentFrequency>(command.NewPaymentFrequency), command.NewStartDate,
            command.NewMaturityDate, command.NewExpectedGrossInterest, command.NewExpectedDeductionRatePercent,
            command.Notes, renewalAdjustmentPostingId, clock.UtcNow);

        predecessor.MarkRenewed(successorId, renewalAdjustmentPostingId, clock.UtcNow);
        fixedDeposits.Add(successor);
        auditLog.Add(FinanceAuditLogEntry.Record(
            tenantId, clock.UtcNow, currentUser.UserId, "FixedDeposit.Renew", nameof(FixedDeposit), predecessorId.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { successorId = successorId.Value, newCertificateNumber, principalDifference })));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fixed Deposit {PredecessorId} renewed as {SuccessorId} '{CertificateNumber}' for tenant {TenantId}",
            predecessorId, successorId, newCertificateNumber, tenantId);

        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return successor.ToDto(fundingAccount.Name, null, null, 0m, 0m, today);
    }
}
