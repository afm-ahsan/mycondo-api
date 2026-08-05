using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Domain.Features.Security.ParcelCustodyEvents;

/// <summary>
/// One append-only row per <see cref="Parcel"/> status transition — the "custody history" the spec
/// requires. Written by the command handler alongside each domain transition, in the same
/// SaveChanges call; never updated or deleted once written.
/// </summary>
public sealed class ParcelCustodyEvent : Entity<ParcelCustodyEventId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public ParcelId ParcelId { get; private set; }
    public ParcelStatus ToStatus { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? PerformedBy { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private ParcelCustodyEvent() { }

    private ParcelCustodyEvent(
        ParcelCustodyEventId id,
        Guid tenantId,
        ParcelId parcelId,
        ParcelStatus toStatus,
        Guid? performedBy,
        string? notes,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        ParcelId = parcelId;
        ToStatus = toStatus;
        OccurredAtUtc = nowUtc;
        PerformedBy = performedBy;
        Notes = notes;
        CreatedAtUtc = nowUtc;
    }

    public static ParcelCustodyEvent Record(
        Guid tenantId, ParcelId parcelId, ParcelStatus toStatus, Guid? performedBy, string? notes, DateTimeOffset nowUtc) =>
        new(ParcelCustodyEventId.New(), tenantId, parcelId, toStatus, performedBy, notes?.Trim(), nowUtc);
}
