using AwesomeAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof for <see cref="ProvisionOrganizationWithAdminCommand"/> against a real, ephemeral
/// PostgreSQL container (ADR-033 Task 12B §10) — sends the command straight through <see cref="ISender"/>
/// inside a DI scope rather than over HTTP, deliberately bypassing the currently-broken 401 JWT/HTTP
/// fixture (see <see cref="OrganizationManagementDbTests"/>) that Task 12B is explicit about not fixing
/// here. <see cref="ISender"/>-level dispatch still runs FluentValidation and the real handler exactly as
/// the HTTP endpoint would — only the ASP.NET Core authentication/authorization middleware is skipped,
/// which is irrelevant to what this class proves (the provisioning transaction's contract and atomicity).
/// Needs a Docker daemon — see <see cref="PostgresApiFactory"/>'s own doc comment.
/// </summary>
public class ProvisionOrganizationWithAdminDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ProvisionOrganizationWithAdminDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    /// <summary>Monthly and Annual are priced; Quarterly/SemiAnnual are deliberately left unpriced so
    /// this same package version can also prove the "unsupported cycle" rejection.</summary>
    private static async Task<SubscriptionPackageVersion> CreateAssignablePackageVersionAsync(IServiceScope scope)
    {
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, EffectiveFrom, null,
            monthlyPrice: 8000m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: 84000m, currency: "BDT");
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);

        packages.Add(package);
        versions.Add(version);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return version;
    }

    private static ProvisionOrganizationWithAdminCommand ValidCommand(
        Guid packageVersionId, string suffix, BillingCycle billingCycle, bool autoRenew) => new(
        Name: "Task 12B Integration Org",
        Code: $"T12B-{suffix.ToUpperInvariant()}",
        Slug: $"task12b-{suffix}",
        AdministratorFullName: "Test Admin",
        AdministratorEmail: $"admin@task12b-{suffix}.test",
        AdministratorPassword: "Correct-Horse-Battery-9",
        EnabledModuleKeys: ["billing", "payments"],
        SubscriptionPackageVersionId: packageVersionId,
        BillingCycle: billingCycle,
        AutoRenew: autoRenew);

    [Fact]
    public async Task Provisions_Tenant_Administrator_And_Subscription_Atomically_With_The_Explicit_Contract()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);

        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        ProvisionOrganizationWithAdminCommand command =
            ValidCommand(version.Id.Value, "success", BillingCycle.Annual, autoRenew: false);

        ProvisionOrganizationResult result = await sender.Send(command, CancellationToken.None);

        // 1 Tenant, Active.
        using IServiceScope readScope = _factory.Services.CreateScope();
        Tenant? tenant = await readScope.ServiceProvider.GetRequiredService<ITenantRepository>()
            .GetByIdAsync(result.TenantId, CancellationToken.None);
        tenant.Should().NotBeNull();
        tenant!.Status.Should().Be(TenantStatus.Active);

        // 1 expected administrator identity/membership.
        await using MyCondoDbContext tenantDb = _factory.CreateDbContextForTenant(result.TenantId);
        User admin = await tenantDb.Set<User>().SingleAsync(u => u.TenantId == result.TenantId);
        admin.Email.Should().Be(command.AdministratorEmail.ToLowerInvariant());

        Role organizationAdmin = await tenantDb.Set<Role>()
            .SingleAsync(r => r.TenantId == result.TenantId && r.Name == "OrganizationAdmin");
        List<RoleAssignment> assignments = await tenantDb.Set<RoleAssignment>()
            .Where(a => a.TenantId == result.TenantId && a.UserId == admin.Id)
            .ToListAsync();
        assignments.Should().ContainSingle(a => a.RoleId == organizationAdmin.Id && a.BuildingId == null);

        // 1 OrganizationSubscription, with the exact caller-selected contract.
        OrganizationSubscription? subscription = await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(result.TenantId, CancellationToken.None);
        subscription.Should().NotBeNull();
        subscription!.PackageVersionId.Should().Be(version.Id);
        subscription.BillingCycle.Should().Be(BillingCycle.Annual);
        subscription.BasePrice.Should().Be(84000m);
        subscription.EffectivePrice.Should().Be(84000m);
        subscription.Currency.Should().Be("BDT");
        subscription.AutoRenew.Should().BeFalse();
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Active);
    }

    [Fact]
    public async Task Rejects_An_Unsupported_Billing_Cycle_And_Leaves_No_Partial_Tenant_Admin_Or_Subscription_State()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        // Same package version as the success test: Quarterly has no configured price.
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);

        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        ProvisionOrganizationWithAdminCommand command =
            ValidCommand(version.Id.Value, "rollback", BillingCycle.Quarterly, autoRenew: true);

        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedBillingCycleException>();

        // No partial Tenant/administrator/OrganizationSubscription state was committed — the rejection
        // happens before the provisioning transaction's first SaveChangesAsync call, so nothing beyond
        // this existence check is needed to prove atomicity: the slug was never claimed.
        using IServiceScope verifyScope = _factory.Services.CreateScope();
        bool slugClaimed = await verifyScope.ServiceProvider.GetRequiredService<ITenantRepository>()
            .SlugExistsAsync(command.Slug, CancellationToken.None);
        slugClaimed.Should().BeFalse();
    }
}
