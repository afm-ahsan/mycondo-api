using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Property.Buildings;

public sealed class Building : AggregateRoot<BuildingId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Address { get; private set; }
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
    }

    private Building(BuildingId id, Guid tenantId, string name, string? address, DateTimeOffset nowUtc)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Address = address;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Building Create(Guid tenantId, string name, string? address, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Building(BuildingId.New(), tenantId, name.Trim(), address?.Trim(), nowUtc);
    }

    public void UpdateDetails(string name, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Address = address?.Trim();
        Version++;
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
