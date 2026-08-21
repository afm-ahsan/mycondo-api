using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.BankReconciliations.DTOs;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.BankReconciliations.Commands.AddReconciliationAdjustment;

public sealed class AddReconciliationAdjustmentCommandHandler(
    IBankStatementLineRepository statementLines,
    IBankReconciliationRepository reconciliations,
    IFinancialAccountRepository financialAccounts,
    IFinancialPostingService financialPosting,
    IFinanceAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AddReconciliationAdjustmentCommandHandler> logger
) : IRequestHandler<AddReconciliationAdjustmentCommand, BankStatementLineDto>
{
    public async ValueTask<BankStatementLineDto> Handle(AddReconciliationAdjustmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BankStatementLineId lineId = new(command.BankStatementLineId);
        BankStatementLine line = await statementLines.GetByIdAsync(lineId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankStatementLine), command.BankStatementLineId);
        if (line.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(BankStatementLine), command.BankStatementLineId);
        }

        BankReconciliation reconciliation = await reconciliations.GetByIdAsync(line.BankReconciliationId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankReconciliation), line.BankReconciliationId.Value);
        FinancialAccount financialAccount = await financialAccounts.GetByIdAsync(reconciliation.FinancialAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), reconciliation.FinancialAccountId.Value);

        if (!Enum.TryParse(command.OtherSideRole, out LedgerAccountType otherSideRole))
        {
            throw new ArgumentException($"'{command.OtherSideRole}' is not a recognized posting role.", nameof(command));
        }

        // A positive statement line increases the bank balance (a deposit/credit not yet on the ledger,
        // e.g. interest earned) — Debit the bank account, Credit the other-side role. A negative line
        // (a bank charge) is the mirror image.
        decimal amount = Math.Abs(line.Amount);
        (LedgerDirection bankDirection, LedgerDirection otherDirection) = line.Amount > 0
            ? (LedgerDirection.Debit, LedgerDirection.Credit)
            : (LedgerDirection.Credit, LedgerDirection.Debit);

        FinancialPostingLine[] lines =
        [
            new FinancialPostingLine(
                LedgerAccountType.CashOrBank, null, bankDirection, amount, ExplicitAccountId: financialAccount.ChartOfAccountId),
            new FinancialPostingLine(otherSideRole, null, otherDirection, amount),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, line.TransactionDate, command.Description, "BankReconciliationAdjustment",
                lineId.Value, lines, financialAccount.FundId),
            cancellationToken);

        line.MarkAdjusted(posted.Posting.Id);
        auditLog.Add(FinanceAuditLogEntry.Record(
            tenantId, clock.UtcNow, currentUser.UserId, "BankReconciliation.Adjustment", nameof(BankStatementLine), lineId.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { otherSideRole = command.OtherSideRole, amount = line.Amount })));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Bank statement line {BankStatementLineId} adjusted via posting {PostingId}, tenant {TenantId}",
            lineId, posted.Posting.Id, tenantId);

        return new BankStatementLineDto(
            line.Id.Value, line.BankReconciliationId.Value, line.TransactionDate, line.Description, line.Amount,
            line.Status.ToString(), line.MatchedLedgerEntryId?.Value, line.AdjustmentPostingId?.Value, line.ResolutionNotes);
    }
}
