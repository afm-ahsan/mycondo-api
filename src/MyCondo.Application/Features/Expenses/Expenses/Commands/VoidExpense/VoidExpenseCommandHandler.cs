using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;

/// <summary>
/// Voids an Expense. While still <see cref="ExpenseStatus.Recorded"/> (never posted) this is a pure
/// status flip, unchanged from pre-Template-3 behaviour. Once <see cref="ExpenseStatus.Posted"/> or
/// <see cref="ExpenseStatus.Paid"/>, posts the reversing ledger entr(y/ies) through the same centralized
/// <see cref="IFinancialPostingService"/> every other Expense posting goes through — Expense never
/// constructs a <c>LedgerPosting</c> itself (root CLAUDE.md's "Expense != Journal Entry" boundary).
/// Reversal amounts/accounts are reconstructed from the Expense's own fields (<see cref="Expense.Amount"/>,
/// <see cref="Expense.IsPaid"/>), the same "reconstruct from the source aggregate, don't look up the
/// original posting's lines" approach <c>ReverseFineCommandHandler</c> uses for Invoices.
/// </summary>
public sealed class VoidExpenseCommandHandler(
    IExpenseRepository expenses,
    IFinancialPostingService financialPosting,
    IFinanceAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<VoidExpenseCommandHandler> logger
) : IRequestHandler<VoidExpenseCommand>
{
    private const string ExpenseManagePermission = "expense.manage";

    public async ValueTask<Unit> Handle(VoidExpenseCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ExpenseId expenseId = new(command.ExpenseId);
        Expense expense = await expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Expense), command.ExpenseId);

        if (expense.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Expense), command.ExpenseId);
        }

        if (!currentUser.HasPermissionForBuilding(ExpenseManagePermission, expense.BuildingId?.Value))
        {
            throw new ForbiddenException("You do not have permission to manage expenses for this Building.");
        }

        if (expense.Status == ExpenseStatus.Voided)
        {
            // Idempotent no-op, same as pre-Template-3 behaviour — nothing more to reverse.
            expense.Void(command.Reason, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        LedgerPostingId? reversalPostingId = null;
        LedgerPostingId? paymentReversalPostingId = null;
        DateOnly businessDate = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        if (expense.Status == ExpenseStatus.Paid && expense.PaymentPostingId is LedgerPostingId paymentPostingId)
        {
            // Unwind the supplier payment first ("Dr AccountsPayable / Cr Bank" was posted at pay time —
            // reverse it "Dr CashOrBank / Cr AccountsPayable").
            FinancialPostingLine[] paymentReversalLines =
            [
                new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, expense.Amount),
                new FinancialPostingLine(LedgerAccountType.AccountsPayable, null, LedgerDirection.Credit, expense.Amount),
            ];

            FinancialPostingResult paymentReversal = await financialPosting.PostAsync(
                new FinancialPostingRequest(
                    tenantId, businessDate, $"Reversal of Expense payment: {command.Reason}", "ExpensePaymentVoid",
                    paymentPostingId.Value, paymentReversalLines, expense.FundId, IsPrivilegedAdjustment: true),
                cancellationToken);

            paymentReversalPostingId = paymentReversal.Posting.Id;
        }

        if (expense.Status is ExpenseStatus.Posted or ExpenseStatus.Paid && expense.PostingId is LedgerPostingId postingId)
        {
            // Unwind the primary posting — "Dr OperatingExpense / Cr AccountsPayable" (unpaid) or
            // "Dr OperatingExpense / Cr CashOrBank" (immediate payment) — reversed symmetrically. Status
            // == Paid is only reachable via the unpaid path (see Expense.MarkPaid's guard), so by the
            // time we're here IsPaid may already be true even though the *primary* posting was originally
            // "Cr AccountsPayable" — only a still-Posted, immediately-paid expense (never went through
            // Paid) originally credited CashOrBank.
            LedgerAccountType creditSide = expense.Status == ExpenseStatus.Posted && expense.IsPaid
                ? LedgerAccountType.CashOrBank
                : LedgerAccountType.AccountsPayable;

            FinancialPostingLine[] reversalLines =
            [
                new FinancialPostingLine(creditSide, null, LedgerDirection.Debit, expense.Amount),
                new FinancialPostingLine(LedgerAccountType.OperatingExpense, null, LedgerDirection.Credit, expense.Amount),
            ];

            FinancialPostingResult reversal = await financialPosting.PostAsync(
                new FinancialPostingRequest(
                    tenantId, businessDate, $"Reversal of Expense: {command.Reason}", "ExpenseVoid",
                    postingId.Value, reversalLines, expense.FundId, IsPrivilegedAdjustment: true),
                cancellationToken);

            reversalPostingId = reversal.Posting.Id;
        }

        expense.Void(command.Reason, clock.UtcNow, reversalPostingId, paymentReversalPostingId);
        auditLog.Add(FinanceAuditLogEntry.Record(
            tenantId, clock.UtcNow, currentUser.UserId, "Expense.Void", nameof(Expense), expenseId.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { reason = command.Reason })));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Expense {ExpenseId} voided for tenant {TenantId}, reversal posting {ReversalPostingId}",
            expenseId, tenantId, reversalPostingId);

        return Unit.Value;
    }
}
