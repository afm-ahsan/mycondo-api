using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Property.Flats;

/// <summary>
/// A unit/flat within a <see cref="Building"/>. Named "Flat" rather than "Unit" — <c>Mediator</c>
/// (this project's CQRS library, see ADR-002) defines its own <c>Unit</c> type for void-returning
/// command handlers, so an entity named <c>Unit</c> would collide via ambiguous reference in any
/// handler file that needs both <c>using Mediator;</c> and this entity's namespace.
/// </summary>
public sealed class Flat : AggregateRoot<FlatId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public string FlatNumber { get; private set; }
    public int? FloorNumber { get; private set; }
    public FlatType FlatType { get; private set; }
    public decimal? AreaSqFt { get; private set; }
    public Guid? PrimaryPhotoAttachmentId { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private Flat()
    {
        FlatNumber = null!;
    }

    private Flat(
        FlatId id,
        Guid tenantId,
        BuildingId buildingId,
        string flatNumber,
        int? floorNumber,
        FlatType flatType,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        FlatNumber = flatNumber;
        FloorNumber = floorNumber;
        FlatType = flatType;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Flat Create(
        Guid tenantId,
        BuildingId buildingId,
        string flatNumber,
        int? floorNumber,
        FlatType flatType,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flatNumber);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Flat(FlatId.New(), tenantId, buildingId, flatNumber.Trim(), floorNumber, flatType, nowUtc);
    }

    public void UpdateDetails(string flatNumber, int? floorNumber, FlatType flatType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flatNumber);

        FlatNumber = flatNumber.Trim();
        FloorNumber = floorNumber;
        FlatType = flatType;
        Version++;
    }

    /// <summary>
    /// Required for a <c>PerSquareFoot</c> service-charge rule to bill this flat — see
    /// <c>Features.Payments.Billing</c>'s batch generation, which skips (never zero-fills) a flat
    /// with no area on record rather than risk an incorrect invoice.
    /// </summary>
    public void SetAreaSqFt(decimal? areaSqFt)
    {
        if (areaSqFt is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaSqFt), "AreaSqFt must be positive when provided.");
        }

        AreaSqFt = areaSqFt;
        Version++;
    }

    public void SetPrimaryPhoto(Guid? photoAttachmentId)
    {
        PrimaryPhotoAttachmentId = photoAttachmentId;
    }

    public void Deactivate(DateTimeOffset nowUtc, Guid? deactivatedBy)
    {
        if (DeletedAtUtc is not null)
        {
            return;
        }

        DeletedAtUtc = nowUtc;
        DeletedBy = deactivatedBy;
    }
}
