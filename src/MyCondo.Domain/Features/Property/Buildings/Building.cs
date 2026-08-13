using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Property.Buildings;

public sealed class Building : AggregateRoot<BuildingId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string? Address { get; private set; }
    public Guid? PrimaryPhotoAttachmentId { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private Building()
    {
        Name = null!;
        Code = null!;
    }

    private Building(
        BuildingId id, Guid tenantId, string name, string code, string? address, DateTimeOffset nowUtc)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        Address = address;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>
    /// <paramref name="code"/> is a short, tenant-unique identifier (e.g. "AISHA") used verbatim in
    /// generated invoice numbers (<c>INV-{code}-{yyyy}-{seq}</c> — see <c>Features.Payments.Billing</c>).
    /// Normalized to uppercase. Mutable via <see cref="UpdateDetails"/> like <see cref="Name"/> — this
    /// is safe for invoice numbering because each <c>Invoice.InvoiceNumber</c> is generated once and
    /// stored as a plain string, never recomputed from the current <see cref="Code"/>, so a later code
    /// change cannot retroactively alter an already-issued invoice number.
    /// </summary>
    public static Building Create(Guid tenantId, string name, string code, string? address, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Building(BuildingId.New(), tenantId, name.Trim(), code.Trim().ToUpperInvariant(), address?.Trim(), nowUtc);
    }

    public void UpdateDetails(string name, string code, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Address = address?.Trim();
        Version++;
    }

    public void SetPrimaryPhoto(Guid? photoAttachmentId)
    {
        PrimaryPhotoAttachmentId = photoAttachmentId;
    }

    /// <summary>
    /// Soft-deletes the building. Picked up by <c>BuildingConfiguration</c>'s
    /// <c>DeletedAtUtc == null</c> query filter — units and gates referencing a deactivated building
    /// are not cascade-deactivated here; that is a deliberate scope boundary for this slice.
    /// </summary>
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
