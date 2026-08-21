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

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.WithdrawFixedDeposit;

/// <summary>
/// Matured (or early) withdrawal, terminal — not a renewal. Posts "Dr CashOrBank / Cr FixedDeposit"
/// (principal returned), reversing the placement's direction, through <see cref="IFinancialPostingService"/>.
/// </summary>
public sealed class WithdrawFixedDepositCommandHandler(
    IFixedDepositRepository fixedDeposits,
    IFixedDepositInterestAccrualRepository accruals,
    IFixedDepositInterestReceiptRepository receipts,
    IFinancialAccountRepository financialAccounts,
    IFinancialPostingService financialPosting,
    IFinanceAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<WithdrawFixedDepositCommandHandler> logger
) : IRequestHandler<WithdrawFixedDepositCommand, FixedDepositDto>
{
    public async ValueTask<FixedDepositDto> Handle(WithdrawFixedDepositCommand command, CancellationToken cancellationToken)
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
            throw new ConflictException($"Fixed Deposit {fixedDeposit.Id} is {fixedDeposit.Status} and cannot be withdrawn.");
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
            throw new ConflictException($"Financial Account {receivingAccount.Id} is inactive and cannot receive a withdrawal.");
        }

        DateOnly accountingDate = command.AccountingDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(
                LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, fixedDeposit.Principal,
                ExplicitAccountId: receivingAccount.ChartOfAccountId),
            new FinancialPostingLine(LedgerAccountType.FixedDeposit, null, LedgerDirection.Credit, fixedDeposit.Principal),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, accountingDate, $"Fixed Deposit withdrawal: {fixedDeposit.CertificateNumber}",
                "FixedDepositMaturity", fixedDepositId.Value, postingLines, fixedDeposit.FundId),
            cancellationToken);

        fixedDeposit.MarkWithdrawn(posted.Posting.Id, receivingAccountId, clock.UtcNow);
        auditLog.Add(FinanceAuditLogEntry.Record(
            tenantId, clock.UtcNow, currentUser.UserId, "FixedDeposit.Withdraw", nameof(FixedDeposit), fixedDepositId.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { principal = fixedDeposit.Principal })));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fixed Deposit {FixedDepositId} withdrawn for tenant {TenantId}, posting {PostingId}",
            fixedDepositId, tenantId, posted.Posting.Id);

        decimal totalAccrued = await accruals.GetTotalAccruedAsync(fixedDepositId, cancellationToken);
        decimal totalReceived = await receipts.GetTotalReceivedGrossAsync(fixedDepositId, cancellationToken);
        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        return fixedDeposit.ToDto(null, receivingAccount.Name, null, totalAccrued, totalReceived, today);
    }
}
