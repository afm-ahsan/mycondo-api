namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>A stable, point-in-time record of who was responsible for a flat when an
/// <see cref="Invoice"/> was issued — captured once at <see cref="Invoice.Issue"/> and never
/// recomputed from live Residents/FlatOwnerships/OccupancyRegistrations data, so a later change of
/// owner or tenant cannot alter an already-issued invoice's historical responsibility. Resolution
/// policy (Application-layer <c>ResponsiblePartyResolver</c>): the flat's active
/// <c>OccupancyRegistration</c> tenant if one exists at issuance time, otherwise the flat's active
/// <c>FlatOwnership</c> owner. Nullable end-to-end — a flat with neither an active tenant nor a
/// recorded owner is not an error; the invoice is still issued and billed against
/// <see cref="Invoice.FlatId"/> exactly as before, just without this optional snapshot.</summary>
public sealed record ResponsiblePartySnapshot(
    ResponsiblePartyType PartyType,
    Guid ResponsibleResidentId,
    Guid? FlatOwnershipId,
    Guid? OccupancyRegistrationId);
