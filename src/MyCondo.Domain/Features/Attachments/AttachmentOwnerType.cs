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
}
