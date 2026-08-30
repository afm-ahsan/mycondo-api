namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>
/// Platform Control Plane billing/collections summary for one currency (ADR-034 Task 14L) — never
/// summed or converted across currencies, same rule <see cref="PlatformOrganizationOutstandingDto"/>
/// already follows. <see cref="InvoicedAmount"/>/<see cref="CollectedAmount"/> are period-flow figures
/// scoped to the report's requested date range (invoice <c>IssueDate</c>/payment <c>PaymentDate</c>
/// respectively); <see cref="OutstandingAmount"/>/<see cref="OverdueOutstandingAmount"/> are always a
/// current snapshot from persisted <c>SubscriptionInvoice.OutstandingAmount</c>, independent of that
/// date range — same current-snapshot-vs-period-flow split the tenant Financial Summary report already
/// established.
/// </summary>
public sealed record PlatformBillingSummaryCurrencyDto(
    string Currency,
    decimal InvoicedAmount,
    int InvoicedInvoiceCount,
    decimal CollectedAmount,
    int CollectedPaymentCount,
    decimal OutstandingAmount,
    int OutstandingInvoiceCount,
    decimal OverdueOutstandingAmount,
    int OverdueInvoiceCount,
    int? MaxDaysOverdue);
