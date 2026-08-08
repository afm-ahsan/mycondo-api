using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
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
/// tie-breaker — see financial-engine.md invariant 5). Any remainder after all outstanding
/// invoices are paid off is not force-allocated — it stays as unapplied credit, already
/// representable via <see cref="ILedgerEntryRepository.GetReceivableBalanceForFlatAsync"/> going
/// negative; no separate "unapplied credit" entity is introduced. Wrapped in an explicit
/// transaction because the outstanding-invoice lock (<c>FOR UPDATE</c>) must be held across the
/// allocation writes and the final commit, not released after the read.
/// </summary>
public sealed class RecordPaymentCommandHandler(
    IResidentAccountRepository accounts,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IPaymentAllocationRepository paymentAllocations,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
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

        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, command.Amount, description),
            new LedgerLine(LedgerAccountType.ResidentReceivable, flatId, LedgerDirection.Credit, command.Amount, description),
        ];

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, command.BusinessDate, description, "Payment", null, lines, nowUtc);

        ledgerPostings.Add(posting);
        ledgerEntries.AddRange(entries);

        Payment payment = Payment.Record(
            tenantId, flatId, command.Amount, method, command.ReferenceNumber, command.BusinessDate,
            currentUser.UserId, posting.Id, nowUtc);
        payments.Add(payment);

        IReadOnlyList<(PaymentAllocation Allocation, string InvoiceNumber)> allocations = await AllocateFifoAsync(
            tenantId, flatId, payment, command.Amount, nowUtc, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} of {Amount} recorded for flat {FlatId}, tenant {TenantId}, allocated across {InvoiceCount} invoice(s)",
            payment.Id, command.Amount, flatId, tenantId, allocations.Count);

        return payment.ToDto(allocations);
    }

    private async Task<IReadOnlyList<(PaymentAllocation Allocation, string InvoiceNumber)>> AllocateFifoAsync(
        Guid tenantId, FlatId flatId, Payment payment, decimal paymentAmount, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        IReadOnlyList<Invoice> outstanding = await invoices.GetOutstandingForFlatForUpdateAsync(tenantId, flatId, cancellationToken);

        List<(PaymentAllocation Allocation, string InvoiceNumber)> allocations = [];
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

            PaymentAllocation allocation = PaymentAllocation.Allocate(tenantId, payment.Id, invoice.Id, flatId, allocatedAmount, nowUtc);
            allocations.Add((allocation, invoice.InvoiceNumber));

            remaining -= allocatedAmount;
        }

        paymentAllocations.AddRange(allocations.Select(a => a.Allocation));

        return allocations;
    }
}
