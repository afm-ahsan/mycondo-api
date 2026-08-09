using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Platform.PlatformUsers;

/// <summary>
/// A platform-scoped identity — MyCondo's own operations staff, not a member of any
/// <see cref="MyCondo.Domain.Features.Tenancy.Tenant"/>. Deliberately has no <c>TenantId</c> anywhere
/// on this aggregate: a <see cref="PlatformUser"/> is not, and must never become, a row that RLS'
/// tenant-isolation policies need to reason about. See mycondo-docs ADR-019.
/// </summary>
public sealed class PlatformUser : AggregateRoot<PlatformUserId>, IAuditable
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string DisplayName { get; private set; }
    public PlatformUserStatus Status { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private PlatformUser()
    {
        Email = null!;
        PasswordHash = null!;
        DisplayName = null!;
    }

    private PlatformUser(
        PlatformUserId id,
        string email,
        string passwordHash,
        string displayName,
        DateTimeOffset nowUtc) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Status = PlatformUserStatus.Active;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Caller is responsible for hashing the password before invoking — same contract as
    /// <see cref="MyCondo.Domain.Features.Identity.Users.User.Register"/>.</summary>
    public static PlatformUser Create(
        string email,
        string passwordHash,
        string displayName,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        string normalizedEmail = email.Trim().ToLowerInvariant();

        return new PlatformUser(
            PlatformUserId.New(), normalizedEmail, passwordHash, displayName.Trim(), nowUtc);
    }

    public void RecordLogin(DateTimeOffset nowUtc)
    {
        LastLoginAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void ChangePassword(string newPasswordHash, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);

        if (string.Equals(PasswordHash, newPasswordHash, StringComparison.Ordinal))
        {
            return;
        }

        PasswordHash = newPasswordHash;
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
