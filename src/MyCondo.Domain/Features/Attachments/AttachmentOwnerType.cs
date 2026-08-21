namespace MyCondo.Domain.Features.Attachments;

/// <summary>
/// Discriminates which entity an <see cref="Attachment"/> belongs to. Append a value here (never a
/// free-text owner-type string) as each later register introduces its own photo/identity-document
/// need — e.g. Guest, Vehicle, DomesticWorker, Parcel.
/// </summary>
public enum AttachmentOwnerType
{
    Resident = 0,
    OccupancyRegistration = 1,
    Building = 2,
    Flat = 3,
    User = 4,
    ResidentHouseholdMember = 5,
    LeasingHouseholdMember = 6,
    /// <summary>Added by Template 3 — an Expense's supporting document (invoice/voucher/receipt).</summary>
    Expense = 7,
    /// <summary>Added by Template 4 — a Fixed Deposit's supporting document (certificate/statement).</summary>
    FixedDeposit = 8,
}
