using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Application.Common;

/// <summary>
/// Days-overdue calculation shared by every Task 14D Platform billing read model — deliberately no
/// aging-bucket classification, since ADR-034 defines no bucket boundaries (see the Task 14D
/// completion report). Overdue is defined purely from the invoice's own persisted
/// <see cref="SubscriptionInvoice.DueDate"/>/<see cref="SubscriptionInvoice.OutstandingAmount"/> —
/// never recalculated from package pricing.
/// </summary>
public static class SubscriptionInvoiceOverdueCalculator
{
    /// <summary>Null when the invoice is not currently collectible-and-overdue (not Issued, no
    /// outstanding balance, or DueDate has not yet passed <paramref name="today"/>); otherwise the
    /// positive day count between <see cref="SubscriptionInvoice.DueDate"/> and <paramref name="today"/>.</summary>
    public static int? DaysOverdue(SubscriptionInvoice invoice, DateOnly today) =>
        invoice.Status == SubscriptionInvoiceStatus.Issued
        && invoice.OutstandingAmount > 0
        && invoice.DueDate < today
            ? today.DayNumber - invoice.DueDate.DayNumber
            : null;
}
