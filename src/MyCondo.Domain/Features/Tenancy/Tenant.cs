using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Tenancy.Events;
using MyCondo.Domain.Features.Tenancy.Exceptions;

namespace MyCondo.Domain.Features.Tenancy;

public sealed class Tenant : AggregateRoot<TenantId>, IAuditable
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public TenantStatus Status { get; private set; }

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
    public static Tenant Provision(string name, string slug, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Tenant tenant = new(TenantId.New(), name.Trim(), slug.Trim().ToLowerInvariant(), nowUtc);

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
}
