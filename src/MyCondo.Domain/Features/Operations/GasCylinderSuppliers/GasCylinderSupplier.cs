using MyCondo.Domain.Common;
using MyCondo.Domain.Common.PhoneNumbers;

namespace MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

/// <summary>Gas cylinder supplier master. Standalone entity, not a dependency on the unimplemented
/// Vendors module — same approach Slice G took for Facility rather than waiting on another module.</summary>
public sealed class GasCylinderSupplier : AggregateRoot<GasCylinderSupplierId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private GasCylinderSupplier()
    {
        Name = null!;
    }

    private GasCylinderSupplier(
        GasCylinderSupplierId id, Guid tenantId, string name, string? contactPhone, string? contactEmail,
        string? address, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        ContactPhone = contactPhone;
        ContactEmail = contactEmail;
        Address = address;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static GasCylinderSupplier Create(
        Guid tenantId, string name, string? contactPhone, string? contactEmail, string? address, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new GasCylinderSupplier(
            GasCylinderSupplierId.New(), tenantId, name.Trim(), BangladeshMobileNumber.Normalize(contactPhone),
            contactEmail?.Trim(), address?.Trim(), nowUtc);
    }

    public void UpdateDetails(string name, string? contactPhone, string? contactEmail, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        ContactPhone = BangladeshMobileNumber.Normalize(contactPhone);
        ContactEmail = contactEmail?.Trim();
        Address = address?.Trim();
        Version++;
    }

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }

    public void Reactivate()
    {
        IsActive = true;
        Version++;
    }
}
