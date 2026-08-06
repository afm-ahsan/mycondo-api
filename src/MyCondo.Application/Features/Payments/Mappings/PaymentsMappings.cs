using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.ResidentAccounts;

namespace MyCondo.Application.Features.Payments.Mappings;

internal static class PaymentsMappings
{
    public static ResidentAccountDto ToDto(this ResidentAccount account) => new(
        account.Id.Value, account.FlatId.Value, account.OpenedAtUtc, account.IsActive);

    public static LedgerEntryDto ToDto(this LedgerEntry entry) => new(
        entry.Id.Value, entry.PostingId.Value, entry.AccountType.ToString(), entry.FlatId?.Value,
        entry.Direction.ToString(), entry.Amount, entry.BusinessDate, entry.Description, entry.CreatedAtUtc);

    public static PaymentDto ToDto(this Payment payment) => new(
        payment.Id.Value, payment.FlatId.Value, payment.Amount, payment.PaymentMethod.ToString(),
        payment.ReferenceNumber, payment.BusinessDate, payment.ReceivedBy, payment.Status.ToString(),
        payment.LedgerPostingId.Value, payment.ReversedAtUtc, payment.ReversedBy, payment.ReversalReason);
}
