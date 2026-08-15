using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.Users.Events;
using MyCondo.Domain.Features.Identity.Users.Exceptions;

namespace MyCondo.Domain.Features.Identity.Users;

public sealed class User : AggregateRoot<UserId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public UserStatus Status { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public Guid? AvatarAttachmentId { get; private set; }
    public int Version { get; private set; }

    // IAuditable
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    // EF Core
    private User()
    {
        Email = null!;
        PasswordHash = null!;
        FullName = null!;
    }

    private User(
        UserId id,
        Guid tenantId,
        string email,
        string passwordHash,
        string fullName,
        string? phoneNumber,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Status = UserStatus.Active;
        EmailConfirmed = false;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Registers a new user. Caller is responsible for hashing the password before invoking.
    /// </summary>
    public static User Register(
        Guid tenantId,
        string email,
        string passwordHash,
        string fullName,
        string? phoneNumber,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        string normalizedEmail = email.Trim().ToLowerInvariant();
        string trimmedName = fullName.Trim();

        User user = new(
            UserId.New(),
            tenantId,
            normalizedEmail,
            passwordHash,
            trimmedName,
            phoneNumber?.Trim(),
            nowUtc);

        user.RaiseDomainEvent(new UserRegisteredEvent(
            user.Id, user.TenantId, user.Email, user.FullName, nowUtc));

        return user;
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

        RaiseDomainEvent(new UserPasswordChangedEvent(Id, TenantId, nowUtc));
    }

    public void RecordLogin(string ipAddress, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);

        LastLoginAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        RaiseDomainEvent(new UserLoggedInEvent(Id, TenantId, ipAddress, nowUtc));
    }

    public void ChangeFullName(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        string trimmed = newName.Trim();
        if (string.Equals(FullName, trimmed, StringComparison.Ordinal))
        {
            return;
        }

        FullName = trimmed;
        Version++;
    }

    /// <summary>
    /// Updates the profile fields an administrator may edit after creation. Email is deliberately
    /// excluded — it is the login identity and changing it is deferred (see mycondo-docs Flat Owner/
    /// User Administration decision log for the rationale).
    /// </summary>
    public void UpdateProfile(string newFullName, string? newPhoneNumber, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newFullName);

        string trimmedName = newFullName.Trim();
        string? trimmedPhone = newPhoneNumber?.Trim();

        if (string.Equals(FullName, trimmedName, StringComparison.Ordinal)
            && string.Equals(PhoneNumber, trimmedPhone, StringComparison.Ordinal))
        {
            return;
        }

        FullName = trimmedName;
        PhoneNumber = trimmedPhone;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void SetAvatar(Guid attachmentId, DateTimeOffset nowUtc)
    {
        AvatarAttachmentId = attachmentId;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void RemoveAvatar(DateTimeOffset nowUtc)
    {
        if (AvatarAttachmentId is null)
        {
            return;
        }

        AvatarAttachmentId = null;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed)
        {
            return;
        }

        EmailConfirmed = true;
        Version++;
    }

    public void Deactivate(DateTimeOffset nowUtc)
    {
        if (Status == UserStatus.Inactive)
        {
            throw new UserAlreadyDeactivatedException(Id);
        }

        Status = UserStatus.Inactive;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc)
    {
        if (Status == UserStatus.Active)
        {
            return;
        }

        Status = UserStatus.Active;
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
