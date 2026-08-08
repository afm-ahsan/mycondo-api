using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.ResidentAccounts;

namespace MyCondo.Application.Features.Payments.Mappings;

internal static class PaymentsMappings
{
    public static ResidentAccountDto ToDto(this ResidentAccount account) => new(
        account.Id.Value, account.FlatId.Value, account.OpenedAtUtc, account.IsActive);

    public static LedgerEntryDto ToDto(this LedgerEntry entry, string? referenceType, Guid? referenceId) => new(
        entry.Id.Value, entry.PostingId.Value, entry.AccountType.ToString(), entry.FlatId?.Value,
        entry.Direction.ToString(), entry.Amount, entry.BusinessDate, entry.Description, entry.CreatedAtUtc,
        referenceType, referenceId);

    public static LedgerEntryDto ToDto(this LedgerEntryWithReference entry) =>
        entry.Entry.ToDto(entry.ReferenceType, entry.ReferenceId);

    public static PaymentAllocationDto ToDto(this PaymentAllocation allocation, string invoiceNumber) => new(
        allocation.Id.Value, allocation.InvoiceId.Value, invoiceNumber, allocation.AllocatedAmount, allocation.AllocatedAtUtc);

    /// <summary>Allocations are paired with the invoice number of the invoice they were applied to —
    /// callers already have the Invoice aggregate in hand wherever a Payment is built (FIFO allocation,
    /// reversal), so no extra query is needed here.</summary>
    public static PaymentDto ToDto(
        this Payment payment,
        IReadOnlyList<(PaymentAllocation Allocation, string InvoiceNumber)>? allocations = null) => new(
        payment.Id.Value, payment.FlatId.Value, payment.Amount, payment.PaymentMethod.ToString(),
        payment.ReferenceNumber, payment.BusinessDate, payment.ReceivedBy, payment.Status.ToString(),
        payment.LedgerPostingId.Value, payment.ReversedAtUtc, payment.ReversedBy, payment.ReversalReason,
        (allocations ?? []).Select(a => a.Allocation.ToDto(a.InvoiceNumber)).ToList());
}
