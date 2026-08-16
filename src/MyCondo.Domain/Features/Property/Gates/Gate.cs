using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Property.Gates;

/// <summary>
/// A named entry/exit point of a building (e.g. "Main Gate", "Basement Ramp"). Referenced by every
/// access-session register (car/guest/staff/teacher in-out, Slice B onward) as the entry/exit gate.
/// Tenant-owned, editable configuration — disable rather than delete once referenced by historical
/// access-session records, mirroring <see cref="Expenses.ExpenseTypes.ExpenseType"/>'s rationale.
/// </summary>
public sealed class Gate : AggregateRoot<GateId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsEntryAllowed { get; private set; }
    public bool IsExitAllowed { get; private set; }
    public int DisplayOrder { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Gate()
    {
        Name = null!;
        Code = null!;
    }

    private Gate(
        GateId id,
        Guid tenantId,
        BuildingId buildingId,
        string name,
        string code,
        string? description,
        bool isEntryAllowed,
        bool isExitAllowed,
        int displayOrder,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        Name = name;
        Code = code;
        Description = description;
        IsActive = true;
        IsEntryAllowed = isEntryAllowed;
        IsExitAllowed = isExitAllowed;
        DisplayOrder = displayOrder;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Gate Create(
        Guid tenantId,
        BuildingId buildingId,
        string name,
        string code,
        string? description,
        bool isEntryAllowed,
        bool isExitAllowed,
        int displayOrder,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Gate(
            GateId.New(), tenantId, buildingId, name.Trim(), code.Trim().ToUpperInvariant(), description?.Trim(),
            isEntryAllowed, isExitAllowed, displayOrder, nowUtc);
    }

    public void Update(
        string name, string code, string? description, bool isEntryAllowed, bool isExitAllowed, int displayOrder,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = description?.Trim();
        IsEntryAllowed = isEntryAllowed;
        IsExitAllowed = isExitAllowed;
        DisplayOrder = displayOrder;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTimeOffset nowUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
