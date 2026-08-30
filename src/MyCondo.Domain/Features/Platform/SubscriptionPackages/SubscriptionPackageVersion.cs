using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

/// <summary>
/// An effective-dated, historically immutable version of a <see cref="SubscriptionPackage"/> (ADR-033 §8)
/// — pricing lives here, on the version, never on the mutable package root. A price change is always a
/// new version (<see cref="Create"/>), never an edit of a live version's price fields; this type exposes
/// no method that can ever change <see cref="MonthlyPrice"/>/<see cref="QuarterlyPrice"/>/
/// <see cref="SemiAnnualPrice"/>/<see cref="AnnualPrice"/>/<see cref="Currency"/> after construction —
/// that immutability is the mechanism preventing the "current price change silently rewrites history"
/// defect ADR-033 §8 names.
///
/// <para><b>"Current version" rule:</b> at most one version per package may be
/// <see cref="SubscriptionPackageVersionStatus.Active"/> at a time, enforced by a partial unique database
/// index (see <c>SubscriptionPackageVersionConfiguration</c>) rather than date-range overlap math —
/// <see cref="Status"/> alone is the deterministic "is this the current version" signal; a package can
/// hold any number of <see cref="SubscriptionPackageVersionStatus.Draft"/> versions with arbitrary,
/// even overlapping, effective-date windows without ambiguity, since none of them is current until
/// explicitly <see cref="Activate"/>d.</para>
/// </summary>
public sealed class SubscriptionPackageVersion : AggregateRoot<SubscriptionPackageVersionId>
{
    public SubscriptionPackageId PackageId { get; private set; }
    public int Version { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveUntil { get; private set; }
    public SubscriptionPackageVersionStatus Status { get; private set; }

    public decimal? MonthlyPrice { get; private set; }
    public decimal? QuarterlyPrice { get; private set; }
    public decimal? SemiAnnualPrice { get; private set; }
    public decimal? AnnualPrice { get; private set; }
    public string Currency { get; private set; }

    private SubscriptionPackageVersion()
    {
        Currency = null!;
    }

    private SubscriptionPackageVersion(
        SubscriptionPackageVersionId id,
        SubscriptionPackageId packageId,
        int version,
        DateOnly effectiveFrom,
        DateOnly? effectiveUntil,
        decimal? monthlyPrice,
        decimal? quarterlyPrice,
        decimal? semiAnnualPrice,
        decimal? annualPrice,
        string currency) : base(id)
    {
        PackageId = packageId;
        Version = version;
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
        Status = SubscriptionPackageVersionStatus.Draft;
        MonthlyPrice = monthlyPrice;
        QuarterlyPrice = quarterlyPrice;
        SemiAnnualPrice = semiAnnualPrice;
        AnnualPrice = annualPrice;
        Currency = currency;
    }

    public static SubscriptionPackageVersion Create(
        SubscriptionPackageId packageId,
        int version,
        DateOnly effectiveFrom,
        DateOnly? effectiveUntil,
        decimal? monthlyPrice,
        decimal? quarterlyPrice,
        decimal? semiAnnualPrice,
        decimal? annualPrice,
        string currency)
    {
        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be a positive, sequential integer.");
        }

        if (effectiveUntil is not null && effectiveUntil < effectiveFrom)
        {
            throw new ArgumentException("EffectiveUntil must not be before EffectiveFrom.", nameof(effectiveUntil));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        ValidatePrice(monthlyPrice, nameof(monthlyPrice));
        ValidatePrice(quarterlyPrice, nameof(quarterlyPrice));
        ValidatePrice(semiAnnualPrice, nameof(semiAnnualPrice));
        ValidatePrice(annualPrice, nameof(annualPrice));

        return new SubscriptionPackageVersion(
            SubscriptionPackageVersionId.New(), packageId, version, effectiveFrom, effectiveUntil,
            monthlyPrice, quarterlyPrice, semiAnnualPrice, annualPrice, currency.Trim());
    }

    private static void ValidatePrice(decimal? price, string paramName)
    {
        if (price is < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Price cannot be negative.");
        }
    }

    /// <summary>Draft → Active only — becomes the package's current commercially offered version. Does
    /// not itself supersede any previously active version of the same package; the caller supersedes the
    /// prior version first (see <see cref="Supersede"/>), enforced at the database level by the partial
    /// unique "one Active version per package" index as the race-condition backstop.</summary>
    public void Activate()
    {
        if (Status != SubscriptionPackageVersionStatus.Draft)
        {
            throw new SubscriptionPackageVersionInvalidTransitionException(
                Id, Status, SubscriptionPackageVersionStatus.Active);
        }

        Status = SubscriptionPackageVersionStatus.Active;
    }

    /// <summary>Active → Superseded, closing <see cref="EffectiveUntil"/> at the supersession date. This
    /// is the only way a version's <see cref="EffectiveUntil"/> ever changes after creation — never a
    /// direct setter — and it never touches price/currency fields, preserving this version's historical
    /// terms exactly as they were while it was active.</summary>
    public void Supersede(DateOnly effectiveUntil)
    {
        if (Status != SubscriptionPackageVersionStatus.Active)
        {
            throw new SubscriptionPackageVersionInvalidTransitionException(
                Id, Status, SubscriptionPackageVersionStatus.Superseded);
        }

        if (effectiveUntil < EffectiveFrom)
        {
            throw new ArgumentException("EffectiveUntil must not be before EffectiveFrom.", nameof(effectiveUntil));
        }

        EffectiveUntil = effectiveUntil;
        Status = SubscriptionPackageVersionStatus.Superseded;
    }
}
