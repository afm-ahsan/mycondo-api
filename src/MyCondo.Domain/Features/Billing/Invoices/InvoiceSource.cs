namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>
/// What generated this invoice. Added in Slice F so a utility bill (metered/fixed electricity or gas
/// charge, posted when a <c>Utilities.Readings.Reading</c> is billed) can reuse the entire
/// <see cref="Invoice"/>/<see cref="InvoiceLine"/>/payment-allocation/void machinery Slice E already
/// built, rather than duplicating it for a second charge type — see ADR-017's sibling decision for
/// Slice F, recorded alongside this change. <see cref="FacilityBooking"/> added in Slice G — a
/// one-off charge, not a recurring-period one, so it is exempted from
/// <c>ux_invoices_tenant_id_flat_id_period_source</c>'s period-uniqueness rule (see
/// <c>Add_Invoice_Source_FacilityBooking</c>); uniqueness for booking invoices is instead guaranteed
/// structurally by <c>Amenities.Bookings.Booking</c>'s own single <c>InvoiceId</c> field.
/// </summary>
public enum InvoiceSource
{
    ServiceCharge = 0,
    Utility = 1,
    FacilityBooking = 2,
    /// <summary>Added by the Billing↔Finance integration template — a fine/penalty assessed against a
    /// flat. Reuses the entire Invoice/InvoiceLine/payment-allocation/void machinery rather than a
    /// parallel entity, same rationale as <see cref="Utility"/>. Like <see cref="FacilityBooking"/>, a
    /// one-off charge exempted from <c>ux_invoices_tenant_id_flat_id_period_source</c>'s
    /// period-uniqueness rule — two fines for the same flat can share a period.</summary>
    Fine = 3,
}
