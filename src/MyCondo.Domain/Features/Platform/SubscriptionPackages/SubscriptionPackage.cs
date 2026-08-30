using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

/// <summary>
/// The stable commercial product identity/root (ADR-033 §7) — e.g. "Professional". Carries no mutable
/// historical pricing or feature composition; those live on <see cref="SubscriptionPackageVersion"/> so
/// that changing current pricing never rewrites history. Lives in the <c>platform</c> schema — not tenant
/// data, no RLS (mycondo-docs ADR-019), same reasoning as <c>FeatureDefinition</c>.
/// </summary>
public sealed class SubscriptionPackage : AggregateRoot<SubscriptionPackageId>
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public SubscriptionPackageStatus Status { get; private set; }
    public SubscriptionPackageVersionId? CurrentVersionId { get; private set; }

    private SubscriptionPackage()
    {
        Code = null!;
        Name = null!;
    }

    private SubscriptionPackage(SubscriptionPackageId id, string code, string name, string? description)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        Status = SubscriptionPackageStatus.Draft;
        CurrentVersionId = null;
    }

    public static SubscriptionPackage Create(string code, string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new SubscriptionPackage(SubscriptionPackageId.New(), code.Trim(), name.Trim(), description?.Trim());
    }

    /// <summary>Draft → Active only. Package names/codes are data (ADR-033 §7); this is a pure
    /// state-machine guard, not a business-policy decision.</summary>
    public void Activate()
    {
        if (Status != SubscriptionPackageStatus.Draft)
        {
            throw new SubscriptionPackageInvalidTransitionException(Id, Status, SubscriptionPackageStatus.Active);
        }

        Status = SubscriptionPackageStatus.Active;
    }

    /// <summary>Draft or Active → Retired. Terminal — a retired package cannot be reactivated (ADR-033
    /// does not define a Retired→Active path; a superseding package is a new commercial product, not a
    /// reactivation of an old one).</summary>
    public void Retire()
    {
        if (Status == SubscriptionPackageStatus.Retired)
        {
            throw new SubscriptionPackageInvalidTransitionException(Id, Status, SubscriptionPackageStatus.Retired);
        }

        Status = SubscriptionPackageStatus.Retired;
    }

    /// <summary>Points at the <see cref="SubscriptionPackageVersion"/> that is currently commercially
    /// offered for this package (ADR-033 §7). A retired package's current version pointer is frozen —
    /// retiring a package is the only way to stop it being offered, never a version change.</summary>
    public void SetCurrentVersion(SubscriptionPackageVersionId versionId)
    {
        if (Status == SubscriptionPackageStatus.Retired)
        {
            throw new SubscriptionPackageRetiredException(Id);
        }

        CurrentVersionId = versionId;
    }
}
