namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>Who an <see cref="Invoice"/>'s <see cref="ResponsiblePartySnapshot"/> identifies as
/// responsible for the flat at issuance time.</summary>
public enum ResponsiblePartyType
{
    Owner = 0,
    Tenant = 1,
}
