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
}
