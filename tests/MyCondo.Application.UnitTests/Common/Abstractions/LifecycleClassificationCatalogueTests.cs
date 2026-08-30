using System.Reflection;
using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.Commands.Login;
using MyCondo.Application.Features.Auth.Commands.Logout;
using MyCondo.Application.Features.Auth.Commands.RefreshToken;
using MyCondo.Application.Features.Auth.Commands.Register;
using MyCondo.Application.Features.Platform.Commands.CancelOrganizationSubscription;
using MyCondo.Application.Features.Platform.Commands.ChangeOrganizationSubscription;
using MyCondo.Application.Features.Platform.Commands.CloseOrganization;
using MyCondo.Application.Features.Platform.Commands.CreateTenantFeatureOverride;
using MyCondo.Application.Features.Platform.Commands.EndTenantFeatureOverride;
using MyCondo.Application.Features.Platform.Commands.ExpireOrganizationSubscription;
using MyCondo.Application.Features.Platform.Commands.MarkOrganizationSubscriptionPastDue;
using MyCondo.Application.Features.Platform.Commands.PlatformLogin;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.Commands.ReactivateOrganization;
using MyCondo.Application.Features.Platform.Commands.ReactivateOrganizationSubscription;
using MyCondo.Application.Features.Platform.Commands.RefreshPlatformToken;
using MyCondo.Application.Features.Platform.Commands.ReplaceOrganizationModules;
using MyCondo.Application.Features.Platform.Commands.RestrictOrganizationSubscription;
using MyCondo.Application.Features.Platform.Commands.RevokePlatformToken;
using MyCondo.Application.Features.Platform.Commands.UpdateOrganization;
using MyCondo.Application.Features.Platform.Commands.UpdateTenantFeatureOverride;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationById;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationFeatureOverrides;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationSubscription;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationSummaryStats;
using MyCondo.Application.Features.Platform.Queries.ExportOrganizationBillingHistory;
using MyCondo.Application.Features.Platform.Queries.ExportPlatformBillingSummary;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationBillingHistory;
using MyCondo.Application.Features.Platform.Queries.GetPlatformBillingSummary;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionPackageOptions;
using MyCondo.Application.Features.Platform.Queries.ListOrganizations;
using MyCondo.Application.Features.Tenancy.Commands.ActivateTenant;
using MyCondo.Application.Features.Tenancy.Commands.SuspendTenant;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

namespace MyCondo.Application.UnitTests.Common.Abstractions;

/// <summary>
/// Structural drift guard for the ADR-032 Task 10A lifecycle read/write classification rollout
/// (Task 10A §35-§38): enumerates every <c>IRequest&lt;&gt;</c>-implementing type in the Application
/// assembly and asserts it is explicitly classified as exactly one of <see cref="ILifecycleReadOperation"/>
/// or <see cref="ILifecycleWriteOperation"/> (<see cref="IBillingResolutionOperation"/> would also count,
/// but no request implements it yet — ADR-034 is not built).
///
/// <para>A small, curated exception list (Task 10A §36 — reflection alone cannot safely tell tenant-scheme
/// requests from Platform-scheme/pre-auth ones) covers requests that structurally never reach
/// <c>TenantLifecycleBehavior</c>'s tenant-scoped branch: pre-auth Auth flows (no <c>ICurrentUserProvider
/// .TenantId</c> yet) and Platform control-plane requests (a Platform JWT structurally carries no
/// <c>TenantId</c> claim, ADR-019). Any new tenant-scoped request added later must be marked Read or Write
/// or this test fails — preventing indefinite reliance on <c>TenantLifecycleBehavior</c>'s fail-closed
/// default for requests that were simply never revisited.</para>
/// </summary>
public class LifecycleClassificationCatalogueTests
{
    private static readonly HashSet<Type> ExemptRequestTypes =
    [
        // Pre-auth / session cleanup: no tenant context exists yet, or the request only tears down a
        // refresh token (ICurrentUserProvider.TenantId is unset or irrelevant at this pipeline stage).
        typeof(LoginCommand),
        typeof(RefreshTokenCommand),
        typeof(RegisterUserCommand),
        typeof(LogoutCommand),

        // Platform control-plane: Platform-scheme JWTs structurally carry no TenantId claim (ADR-019),
        // so these never reach TenantLifecycleBehavior's tenant-scoped branch.
        typeof(PlatformLoginCommand),
        typeof(ProvisionOrganizationWithAdminCommand),
        typeof(RefreshPlatformTokenCommand),
        typeof(RevokePlatformTokenCommand),
        typeof(GetOrganizationByIdQuery),
        typeof(GetOrganizationSummaryStatsQuery),
        typeof(ListOrganizationsQuery),
        typeof(GetSubscriptionPackageOptionsQuery),
        typeof(CloseOrganizationCommand),
        typeof(ReactivateOrganizationCommand),
        typeof(ReplaceOrganizationModulesCommand),
        typeof(UpdateOrganizationCommand),

        // Platform subscription/entitlement administration (ADR-033 Tasks 13A-13D): platform.subscription.*
        // and platform.support.access permissions, authenticated exclusively via RequirePlatformPermission —
        // same Platform-scheme/no-TenantId-claim reasoning as the rest of this group.
        typeof(GetOrganizationSubscriptionQuery),
        typeof(GetOrganizationFeatureOverridesQuery),
        typeof(ChangeOrganizationSubscriptionCommand),
        typeof(CancelOrganizationSubscriptionCommand),
        typeof(ExpireOrganizationSubscriptionCommand),
        typeof(MarkOrganizationSubscriptionPastDueCommand),
        typeof(ReactivateOrganizationSubscriptionCommand),
        typeof(RestrictOrganizationSubscriptionCommand),
        typeof(CreateTenantFeatureOverrideCommand),
        typeof(UpdateTenantFeatureOverrideCommand),
        typeof(EndTenantFeatureOverrideCommand),

        // Platform billing reporting (ADR-034 Task 14L): platform.subscription.read-gated, same
        // Platform-scheme/no-TenantId-claim reasoning as the rest of this group.
        typeof(GetPlatformBillingSummaryQuery),
        typeof(GetOrganizationBillingHistoryQuery),
        typeof(ExportPlatformBillingSummaryQuery),
        typeof(ExportOrganizationBillingHistoryQuery),

        // Organization lifecycle control itself (sets the TenantStatus TenantLifecycleBehavior reads) —
        // a platform-administered action, not a tenant-scoped business operation.
        typeof(ActivateTenantCommand),
        typeof(SuspendTenantCommand),

        // Pre-auth tenant resolution by slug (e.g. rendering a tenant-branded login page) — no
        // ICurrentUserProvider/TenantId dependency in the handler.
        typeof(GetTenantBySlugQuery)
    ];

    private static IReadOnlyList<Type> AllRequestTypes { get; } = typeof(DependencyInjection).Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && !t.IsInterface && ImplementsIRequest(t))
        .ToList();

    private static bool ImplementsIRequest(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

    [Fact]
    public void At_Least_One_Request_Type_Exists()
    {
        // Guards against this test silently passing vacuously if the Application assembly were ever
        // restructured such that no IRequest<> types were discoverable by reflection.
        AllRequestTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Every_Exempt_Request_Type_Still_Exists_And_Is_Unmarked()
    {
        // Catches a stale exemption: if a listed exception is renamed/removed, or is later marked Read/Write
        // (e.g. after an architectural change makes it tenant-scoped), this test should fail so the
        // exemption list is revisited rather than silently masking a real request.
        foreach (Type exempt in ExemptRequestTypes)
        {
            AllRequestTypes.Should().Contain(exempt, $"{exempt.FullName} is a listed exemption and must still exist");
            typeof(ILifecycleReadOperation).IsAssignableFrom(exempt).Should().BeFalse(
                $"{exempt.FullName} is listed as exempt but now implements {nameof(ILifecycleReadOperation)} — remove it from the exemption list instead");
            typeof(ILifecycleWriteOperation).IsAssignableFrom(exempt).Should().BeFalse(
                $"{exempt.FullName} is listed as exempt but now implements {nameof(ILifecycleWriteOperation)} — remove it from the exemption list instead");
        }
    }

    [Fact]
    public void Every_Non_Exempt_Request_Is_Classified_As_Read_Write_Or_BillingResolution()
    {
        IEnumerable<Type> unclassified = AllRequestTypes
            .Where(t => !ExemptRequestTypes.Contains(t))
            .Where(t => !typeof(ILifecycleReadOperation).IsAssignableFrom(t)
                && !typeof(ILifecycleWriteOperation).IsAssignableFrom(t)
                && !typeof(IBillingResolutionOperation).IsAssignableFrom(t));

        unclassified.Should().BeEmpty(
            "every tenant-scoped request must explicitly implement ILifecycleReadOperation or " +
            "ILifecycleWriteOperation (ADR-032 Task 10A) unless added to the curated exemption list above");
    }

    [Fact]
    public void No_Request_Implements_Both_Read_And_Write()
    {
        IEnumerable<Type> both = AllRequestTypes
            .Where(t => typeof(ILifecycleReadOperation).IsAssignableFrom(t) && typeof(ILifecycleWriteOperation).IsAssignableFrom(t));

        both.Should().BeEmpty("a request cannot be simultaneously a lifecycle-safe read and a blocked write");
    }
}
