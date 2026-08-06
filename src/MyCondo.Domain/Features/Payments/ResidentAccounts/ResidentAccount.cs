using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.ResidentAccounts;

/// <summary>
/// A registry entry proving a flat has been onboarded to the ledger — one per flat, created before
/// any charge/payment/opening-balance posting is allowed against that flat. Carries no balance field;
/// balance is always derived from <see cref="Ledger.LedgerEntry"/> rows for the flat.
/// </summary>
public sealed class ResidentAccount : AggregateRoot<ResidentAccountId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FlatId FlatId { get; private set; }
    public DateTimeOffset OpenedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private ResidentAccount() { }

    private ResidentAccount(ResidentAccountId id, Guid tenantId, FlatId flatId, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FlatId = flatId;
        OpenedAtUtc = nowUtc;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static ResidentAccount Open(Guid tenantId, FlatId flatId, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new ResidentAccount(ResidentAccountId.New(), tenantId, flatId, nowUtc);
    }
}
