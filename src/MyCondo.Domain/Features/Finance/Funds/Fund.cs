using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Finance.Funds;

/// <summary>A tenant-configurable fund (e.g. Accumulated Association Fund, Reserve Fund) — an optional
/// dimension a <c>LedgerEntry</c> can be tagged with. Not required by any posting role today; exists so
/// journal lines can be attributed to a fund once a call site needs to (e.g. reserve-fund contributions
/// in a later Finance template).</summary>
public sealed class Fund : AggregateRoot<FundId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Fund()
    {
        Code = null!;
        Name = null!;
    }

    private Fund(FundId id, Guid tenantId, string code, string name, string? description) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
    }

    public static Fund Create(Guid tenantId, string code, string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Fund(FundId.New(), tenantId, code.Trim(), name.Trim(), description?.Trim());
    }

    public void Deactivate() => IsActive = false;
}
