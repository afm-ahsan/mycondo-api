using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Tenancy.Events;
using MyCondo.Domain.Features.Tenancy.Exceptions;

namespace MyCondo.Domain.Features.Tenancy;

public sealed class Tenant : AggregateRoot<TenantId>, IAuditable
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Code { get; private set; }
    public TenantStatus Status { get; private set; }

    /// <summary>
    /// Denormalized snapshot of the organization's founding administrator, captured once at
    /// provisioning time. This intentionally avoids a cross-schema join against RLS-protected
    /// <c>identity.users</c> from Platform-tier read queries (Platform requests have no tenant
    /// context to satisfy that table's FORCE RLS policy — see <see cref="SetPrimaryAdministrator"/>).
    /// Not updated if the admin's name/email later changes tenant-side; acceptable staleness for an
    /// MVP "who founded this org" display field, not a live directory lookup.
    /// </summary>
    public Guid? PrimaryAdministratorUserId { get; private set; }
    public string? PrimaryAdministratorFullName { get; private set; }
    public string? PrimaryAdministratorEmail { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Tenant()
    {
        Name = null!;
        Slug = null!;
    }

    private Tenant(TenantId id, string name, string slug, DateTimeOffset nowUtc) : base(id)
    {
        Name = name;
        Slug = slug;
        Status = TenantStatus.PendingActivation;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Provisions a new tenant in PendingActivation status. Caller is responsible for normalizing
    /// and validating the slug's shape (lowercase, hyphenated) before invoking — see
    /// ProvisionTenantCommandValidator.
    /// </summary>
    public static Tenant Provision(string name, string slug, DateTimeOffset nowUtc) =>
        Provision(TenantId.New(), name, slug, nowUtc);

    /// <summary>
    /// Overload for callers that must know the tenant's id before this call returns — e.g. Platform's
    /// organization+admin provisioning, which needs the id up front to declare the correct RLS tenant
    /// context for the same-transaction admin-user write. See ProvisionOrganizationWithAdminCommandHandler.
    /// </summary>
    public static Tenant Provision(TenantId id, string name, string slug, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Tenant tenant = new(id, name.Trim(), slug.Trim().ToLowerInvariant(), nowUtc);

        tenant.RaiseDomainEvent(new TenantProvisionedEvent(
            tenant.Id, tenant.Name, tenant.Slug, nowUtc));

        return tenant;
    }

    public void Activate(DateTimeOffset nowUtc)
    {
        if (Status == TenantStatus.Active)
        {
            return;
        }

        if (Status == TenantStatus.Suspended)
        {
            throw new InvalidTenantStatusTransitionException(Id, Status, TenantStatus.Active);
        }

        Status = TenantStatus.Active;
        UpdatedAtUtc = nowUtc;
        RaiseDomainEvent(new TenantActivatedEvent(Id, nowUtc));
    }

    public void Suspend(DateTimeOffset nowUtc)
    {
        if (Status == TenantStatus.Suspended)
        {
            return;
        }

        if (Status == TenantStatus.PendingActivation)
        {
            throw new InvalidTenantStatusTransitionException(Id, Status, TenantStatus.Suspended);
        }

        Status = TenantStatus.Suspended;
        UpdatedAtUtc = nowUtc;
        RaiseDomainEvent(new TenantSuspendedEvent(Id, nowUtc));
    }

    /// <summary>
    /// Restores a Suspended tenant to Active. Deliberately separate from <see cref="Activate"/> (which
    /// only accepts PendingActivation→Active) so the Platform-only "reactivate" capability never changes
    /// the behavior of the pre-existing tenant-side activate endpoint.
    /// </summary>
    public void Reactivate(DateTimeOffset nowUtc)
    {
        if (Status != TenantStatus.Suspended)
        {
            throw new InvalidTenantStatusTransitionException(Id, Status, TenantStatus.Active);
        }

        Status = TenantStatus.Active;
        UpdatedAtUtc = nowUtc;
        RaiseDomainEvent(new TenantActivatedEvent(Id, nowUtc));
    }

    /// <summary>
    /// Updates Platform-administered organization metadata (name/code). Never touches Slug — the
    /// slug is immutable post-provisioning since it's the tenant sign-in identifier.
    /// </summary>
    public void UpdateDetails(string name, string? code, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Records the founding administrator snapshot. Called exactly once, at provisioning time, by
    /// the same command that creates that user — never a general "reassign admin" operation.
    /// </summary>
    public void SetPrimaryAdministrator(Guid userId, string fullName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        PrimaryAdministratorUserId = userId;
        PrimaryAdministratorFullName = fullName;
        PrimaryAdministratorEmail = email;
    }
}
