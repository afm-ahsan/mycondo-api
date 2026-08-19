using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Payments.Commands.RecordPayment;

/// <summary>
/// Records a payment and immediately allocates it FIFO against the flat's outstanding invoices
/// (oldest due date first, then invoice date, then invoice number as the final deterministic
/// tie-breaker — see financial-engine.md invariant 5). Any remainder after all outstanding invoices
/// are paid off is posted as <see cref="LedgerAccountType.ResidentAdvance"/> (Billing↔Finance
/// integration template §12) rather than left as unapplied credit implicit in a negative
/// <see cref="LedgerAccountType.ResidentReceivable"/> balance — no separate "advance" entity is
/// introduced, the balance is still ledger-derived, same as the receivable itself. Wrapped in an
/// explicit transaction because the outstanding-invoice lock (<c>FOR UPDATE</c>) must be held across
/// the allocation writes and the final commit, not released after the read.
/// </summary>
public sealed class RecordPaymentCommandHandler(
    IResidentAccountRepository accounts,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IPaymentAllocationRepository paymentAllocations,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordPaymentCommandHandler> logger
) : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    public async ValueTask<PaymentDto> Handle(RecordPaymentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        ResidentAccount? account = await accounts.GetByFlatIdAsync(tenantId, flatId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException(nameof(ResidentAccount), command.FlatId);
        }

        PaymentMethod method = Enum.Parse<PaymentMethod>(command.PaymentMethod);
        string description = string.IsNullOrWhiteSpace(command.Description)
            ? $"Payment received from flat {command.FlatId}"
            : command.Description;
        DateTimeOffset nowUtc = clock.UtcNow;

        await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        // Computed first (mutating outstanding invoices' AmountPaid/Status in memory, not yet saved)
        // so the ledger posting below can split the credit side accurately between ResidentReceivable
        // (what actually settled an obligation) and ResidentAdvance (what didn't) in one balanced
        // posting, rather than always crediting ResidentReceivable for the full amount and letting it
        // go negative on overpayment.
        (List<(InvoiceId InvoiceId, string InvoiceNumber, decimal Amount)> invoiceAllocations, decimal remaining) =
            await AllocateFifoAsync(tenantId, flatId, command.Amount, cancellationToken);
        decimal allocatedToReceivable = command.Amount - remaining;

        List<FinancialPostingLine> lines = [new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, command.Amount)];
        if (allocatedToReceivable > 0)
        {
            lines.Add(new FinancialPostingLine(LedgerAccountType.ResidentReceivable, flatId, LedgerDirection.Credit, allocatedToReceivable));
        }

        if (remaining > 0)
        {
            lines.Add(new FinancialPostingLine(LedgerAccountType.ResidentAdvance, flatId, LedgerDirection.Credit, remaining));
        }

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(tenantId, command.BusinessDate, description, "Payment", null, lines),
            cancellationToken);

        Payment payment = Payment.Record(
            tenantId, flatId, command.Amount, method, command.ReferenceNumber, command.BusinessDate,
            currentUser.UserId, posted.Posting.Id, nowUtc);
        payments.Add(payment);

        List<(PaymentAllocation Allocation, string InvoiceNumber)> allocations = invoiceAllocations
            .Select(a => (PaymentAllocation.Allocate(tenantId, payment.Id, a.InvoiceId, flatId, a.Amount, nowUtc), a.InvoiceNumber))
            .ToList();
        paymentAllocations.AddRange(allocations.Select(a => a.Item1));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} of {Amount} recorded for flat {FlatId}, tenant {TenantId}, allocated across {InvoiceCount} invoice(s), {Advance} posted as resident advance",
            payment.Id, command.Amount, flatId, tenantId, allocations.Count, remaining);

        return payment.ToDto(allocations.Select(a => (a.Item1, a.InvoiceNumber)).ToList());
    }

    /// <summary>Applies the FIFO split against the flat's outstanding invoices (oldest due date
    /// first — see financial-engine.md invariant 5), mutating each invoice's AmountPaid/Status in
    /// memory via <see cref="Invoice.ApplyPayment"/>. Returns the per-invoice amounts (for building
    /// <see cref="PaymentAllocation"/> rows once the Payment exists) plus whatever remains unallocated
    /// after every outstanding invoice is settled.</summary>
    private async Task<(List<(InvoiceId InvoiceId, string InvoiceNumber, decimal Amount)> Allocations, decimal Remaining)> AllocateFifoAsync(
        Guid tenantId, FlatId flatId, decimal paymentAmount, CancellationToken cancellationToken)
    {
        IReadOnlyList<Invoice> outstanding = await invoices.GetOutstandingForFlatForUpdateAsync(tenantId, flatId, cancellationToken);

        List<(InvoiceId InvoiceId, string InvoiceNumber, decimal Amount)> allocations = [];
        decimal remaining = paymentAmount;

        foreach (Invoice invoice in outstanding)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (invoice.FlatId != flatId)
            {
                throw new InvalidOperationException(
                    $"Outstanding-invoice query returned invoice {invoice.Id} for flat {invoice.FlatId}, expected {flatId}.");
            }

            decimal allocatedAmount = Math.Min(remaining, invoice.Balance);
            invoice.ApplyPayment(allocatedAmount);

            allocations.Add((invoice.Id, invoice.InvoiceNumber, allocatedAmount));

            remaining -= allocatedAmount;
        }

        return (allocations, remaining);
    }
}
