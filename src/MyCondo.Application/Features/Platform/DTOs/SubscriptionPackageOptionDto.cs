namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>One commercially assignable package/current-version choice for organization provisioning
/// (ADR-033 Task 13E.1). Only ever reflects a package/version pair the provisioning command would
/// actually accept — see <c>GetSubscriptionPackageOptionsQueryHandler</c> for the eligibility rules
/// mirrored from <c>ProvisionOrganizationWithAdminCommandHandler</c>.</summary>
public sealed record SubscriptionPackageOptionDto(
    Guid PackageId,
    string PackageCode,
    string PackageName,
    Guid PackageVersionId,
    int Version,
    string Currency,
    IReadOnlyList<SubscriptionPackageBillingCycleOptionDto> BillingCycles);

/// <summary>One billing cycle actually priced on a package version — a cycle absent from this list has
/// no price on the version and must not be offered/submitted for it.</summary>
public sealed record SubscriptionPackageBillingCycleOptionDto(
    string BillingCycle,
    decimal Price);
