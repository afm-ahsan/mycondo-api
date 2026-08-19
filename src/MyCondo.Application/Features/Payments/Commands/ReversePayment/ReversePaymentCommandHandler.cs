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
using MyCondo.Domain.Features.Payments.Payments.Exceptions;

namespace MyCondo.Application.Features.Payments.Commands.ReversePayment;

/// <summary>
/// Reverses a payment AND the effect of its FIFO allocations on the invoices they were applied
/// to — the two must never drift apart (a reversed payment with invoices still showing as paid is a
/// financial-integrity bug, not a cosmetic one). Wrapped in an explicit transaction because the
/// invoice mutations and the reversing ledger posting must commit atomically, same reasoning as
/// <c>RecordPaymentCommandHandler</c>'s own transaction. <see cref="Payment.Reverse"/> is called
/// first so a double-reversal attempt (<see cref="PaymentAlreadyReversedException"/>) fails before
/// any invoice is touched.
/// </summary>
public sealed class ReversePaymentCommandHandler(
    IPaymentRepository payments,
    IPaymentAllocationRepository paymentAllocations,
    IInvoiceRepository invoices,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ReversePaymentCommandHandler> logger
) : IRequestHandler<ReversePaymentCommand, PaymentDto>
{
    public async ValueTask<PaymentDto> Handle(ReversePaymentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PaymentId id = new(command.PaymentId);
        Payment payment = await payments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), command.PaymentId);
        if (payment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Payment), command.PaymentId);
        }

        await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        DateTimeOffset nowUtc = clock.UtcNow;
        payment.Reverse(command.Reason, currentUser.UserId, nowUtc);

        IReadOnlyList<PaymentAllocation> allocations = await paymentAllocations.GetForPaymentAsync(id, cancellationToken);
        List<(PaymentAllocation Allocation, string InvoiceNumber)> allocationsWithInvoiceNumbers = [];
        foreach (PaymentAllocation allocation in allocations)
        {
            Invoice invoice = await invoices.GetByIdAsync(allocation.InvoiceId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Invoice {allocation.InvoiceId} referenced by allocation {allocation.Id} not found.");
            if (invoice.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"Invoice {allocation.InvoiceId} referenced by allocation {allocation.Id} belongs to a different tenant.");
            }

            invoice.ReverseAppliedPayment(allocation.AllocatedAmount);
            allocationsWithInvoiceNumbers.Add((allocation, invoice.InvoiceNumber));
        }

        string description = $"Reversal of payment {payment.Id}: {command.Reason}";
        FinancialPostingLine[] lines =
        [
            new FinancialPostingLine(LedgerAccountType.ResidentReceivable, payment.FlatId, LedgerDirection.Debit, payment.Amount),
            new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, payment.Amount),
        ];

        FinancialPostingResult reversal = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "PaymentReversal",
                payment.LedgerPostingId.Value, lines),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} reversed for tenant {TenantId}, {InvoiceCount} invoice(s) restored to outstanding, reversal posting {PostingId}",
            payment.Id, tenantId, allocations.Count, reversal.Posting.Id);

        return payment.ToDto(allocationsWithInvoiceNumbers);
    }
}
