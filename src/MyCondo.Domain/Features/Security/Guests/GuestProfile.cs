using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Security.Guests;

/// <summary>
/// The reusable person record for a guest — created (or reused) once, then referenced by every visit
/// (<see cref="Features.Security.AccessSessions.AccessSession"/>) instead of retyping name/phone/identity
/// details per visit. Block status lives here, not per-visit, so a blocked guest stays blocked across
/// every future visit attempt until explicitly unblocked.
/// </summary>
public sealed class GuestProfile : AggregateRoot<GuestProfileId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string FullName { get; private set; }
    public string Phone { get; private set; }
    public string? IdentityDocumentType { get; private set; }
    public string? IdentityDocumentNumber { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? BlockReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private GuestProfile()
    {
        FullName = null!;
        Phone = null!;
    }

    private GuestProfile(
        GuestProfileId id,
        Guid tenantId,
        string fullName,
        string phone,
        string? identityDocumentType,
        string? identityDocumentNumber,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Phone = phone;
        IdentityDocumentType = identityDocumentType;
        IdentityDocumentNumber = identityDocumentNumber;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static GuestProfile Register(
        Guid tenantId,
        string fullName,
        string phone,
        string? identityDocumentType,
        string? identityDocumentNumber,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new GuestProfile(
            GuestProfileId.New(), tenantId, fullName.Trim(), phone.Trim(), identityDocumentType?.Trim(),
            identityDocumentNumber?.Trim(), nowUtc);
    }

    public void UpdateDetails(string fullName, string? identityDocumentType, string? identityDocumentNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        FullName = fullName.Trim();
        IdentityDocumentType = identityDocumentType?.Trim();
        IdentityDocumentNumber = identityDocumentNumber?.Trim();
        Version++;
    }

    public void Block(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsBlocked = true;
        BlockReason = reason.Trim();
        Version++;
    }

    public void Unblock()
    {
        IsBlocked = false;
        BlockReason = null;
        Version++;
    }
}
