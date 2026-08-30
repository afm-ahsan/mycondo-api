using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyCondo.Api.Endpoints;
using MyCondo.Application.Features.Platform.Commands.GenerateSubscriptionInvoice;
using MyCondo.Application.Features.Platform.Commands.MarkOrganizationSubscriptionPastDue;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.Commands.UpdateOrganization;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// ADR-034 Task H-01: proves the Platform Control Plane mutation-then-audit atomicity fix against a
/// real, ephemeral PostgreSQL container — same <see cref="PostgresApiFactory"/>/HTTP/JWT technique
/// <see cref="ApplyOrganizationSubscriptionBillingDecisionDbTests"/> already establishes (deliberately
/// not <see cref="OrganizationManagementDbTests"/>'s broken fixture, which is separately tracked as
/// H-02). Before this fix, every mutating endpoint in <see cref="PlatformOrganizationEndpoints"/>
/// dispatched its command (which commits the mutation via its own internal
/// <see cref="IUnitOfWork.SaveChangesAsync"/>), then separately added and saved a
/// <see cref="PlatformAuditLogEntry"/> — two independent commits with a window between them in which a
/// crash could persist the mutation without its audit record. The fix wraps both saves in one explicit
/// <see cref="IUnitOfWork.BeginTransactionAsync"/>/<see cref="IUnitOfWorkTransaction.CommitAsync"/>
/// boundary (organization creation is the one exception — see
/// <see cref="ProvisionOrganizationWithAdminCommandHandler"/>'s own class doc comment for why its audit
/// write instead moves inside the handler, onto its tenant-scoped connection). Needs a Docker daemon —
/// see <see cref="PostgresApiFactory"/>.
/// </summary>
public class PlatformMutationAuditAtomicityDbTests : IClassFixture<PostgresApiFactory>
{
    private const string Issuer = "https://api.condobd.com";
    private const string PlatformAudience = "https://platform.condobd.com";
    private const string SigningKey = "test-only-signing-key-not-for-any-real-environment";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    private readonly PostgresApiFactory _factory;

    public PlatformMutationAuditAtomicityDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static string CreatePlatformToken(params string[] permissions)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SigningKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())];
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = PlatformAudience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = creds,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private HttpClient CreatePlatformClient(params string[] permissions)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreatePlatformToken(permissions));
        return client;
    }

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

    private static async Task<Guid> ProvisionOrganizationAsync(IServiceScope scope, Guid packageVersionId, string suffix)
    {
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        ProvisionOrganizationResult result = await sender.Send(
            new ProvisionOrganizationWithAdminCommand(
                Name: "Task H-01 Integration Org",
                Code: $"H01-{suffix.ToUpperInvariant()}",
                Slug: $"task-h01-{suffix}",
                AdministratorFullName: "Test Admin",
                AdministratorEmail: $"admin@task-h01-{suffix}.test",
                AdministratorPassword: "Correct-Horse-Battery-9",
                EnabledModuleKeys: ["billing", "payments"],
                SubscriptionPackageVersionId: packageVersionId,
                BillingCycle: BillingCycle.Monthly,
                AutoRenew: false),
            CancellationToken.None);

        return result.TenantId;
    }

    private static async Task<Guid> ProvisionOrganizationEndToEndAsync(PostgresApiFactory factory, string suffix)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(scope);
        return await ProvisionOrganizationAsync(scope, version.Id.Value, suffix);
    }

    private static Task<List<PlatformAuditLogEntry>> ReadAuditEntriesAsync(IServiceScope scope, Guid tenantId, string action) =>
        scope.ServiceProvider.GetRequiredService<MyCondoDbContext>()
            .Set<PlatformAuditLogEntry>()
            .Where(e => e.TenantId == tenantId && e.Action == action)
            .ToListAsync();

    /// <summary>Item 6: representative organization-management mutation — mutation and audit both land
    /// together over a real HTTP round-trip through the fixed endpoint.</summary>
    [Fact]
    public async Task Organization_Update_Over_Http_Persists_Both_The_Rename_And_Its_Audit_Record()
    {
        Guid tenantId = await ProvisionOrganizationEndToEndAsync(_factory, "org-update");

        using HttpClient client = CreatePlatformClient("platform.organization.update");
        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/v1/platform/organizations/{tenantId}",
            new UpdateOrganizationRequest("Renamed By Task H-01", null));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using IServiceScope readScope = _factory.Services.CreateScope();
        Tenant tenant = (await readScope.ServiceProvider.GetRequiredService<ITenantRepository>()
            .GetByIdAsync(tenantId, CancellationToken.None))!;
        tenant.Name.Should().Be("Renamed By Task H-01");

        List<PlatformAuditLogEntry> auditEntries =
            await ReadAuditEntriesAsync(readScope, tenantId, "platform.organization.updated");
        auditEntries.Should().ContainSingle();
        auditEntries[0].TargetType.Should().Be("Tenant");
        auditEntries[0].TargetId.Should().Be(tenantId.ToString());
    }

    /// <summary>Item 6 (subscription administration variant): a Platform-scope subscription-lifecycle
    /// mutation with no dependent setup, so it isolates the atomicity wrap itself rather than any
    /// billing-decision-specific logic already covered by
    /// <see cref="ApplyOrganizationSubscriptionBillingDecisionDbTests"/>.</summary>
    [Fact]
    public async Task Mark_Subscription_Past_Due_Over_Http_Persists_Both_The_Status_Change_And_Its_Audit_Record()
    {
        Guid tenantId = await ProvisionOrganizationEndToEndAsync(_factory, "mark-past-due");

        using HttpClient client = CreatePlatformClient("platform.subscription.manage");
        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/platform/organizations/{tenantId}/subscription/mark-past-due", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription subscription = (await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetLatestForTenantAsync(tenantId, CancellationToken.None))!;
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.PastDue);

        List<PlatformAuditLogEntry> auditEntries =
            await ReadAuditEntriesAsync(readScope, tenantId, "platform.subscription.marked-past-due");
        auditEntries.Should().ContainSingle();
        auditEntries[0].TargetType.Should().Be("OrganizationSubscription");
    }

    /// <summary>Item 7: representative Platform Billing mutation.</summary>
    [Fact]
    public async Task Generate_Subscription_Invoice_Over_Http_Persists_Both_The_Invoice_And_Its_Audit_Record()
    {
        Guid tenantId = await ProvisionOrganizationEndToEndAsync(_factory, "gen-invoice");

        using HttpClient client = CreatePlatformClient("platform.subscription.manage");
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{tenantId}/subscription/invoices",
            new GenerateSubscriptionInvoiceRequest(new DateOnly(2026, 3, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GenerateSubscriptionInvoiceResponse? result =
            await response.Content.ReadFromJsonAsync<GenerateSubscriptionInvoiceResponse>(JsonOptions);
        result.Should().NotBeNull();

        List<PlatformAuditLogEntry> auditEntries =
            (await _factory.Services.CreateScope().ServiceProvider
                .GetRequiredService<MyCondoDbContext>()
                .Set<PlatformAuditLogEntry>()
                .Where(e => e.TenantId == tenantId && e.Action == "platform.subscription.invoice.generated")
                .ToListAsync());
        auditEntries.Should().ContainSingle();
        auditEntries[0].TargetId.Should().Be(result!.InvoiceId.ToString());
    }

    /// <summary>Organization creation is the one confirmed mutation that does not go through the
    /// ambient endpoint-level transaction (its mutation commits on a separate tenant-scoped connection —
    /// see <see cref="ProvisionOrganizationWithAdminCommandHandler"/>'s class doc comment) — proves its
    /// handler-owned audit write still lands atomically with the tenant it describes.</summary>
    [Fact]
    public async Task Provisioning_An_Organization_Persists_Both_The_Tenant_And_Its_Creation_Audit_Record()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, version.Id.Value, "provision-audit");

        using IServiceScope readScope = _factory.Services.CreateScope();
        Tenant tenant = (await readScope.ServiceProvider.GetRequiredService<ITenantRepository>()
            .GetByIdAsync(tenantId, CancellationToken.None))!;
        tenant.Should().NotBeNull();

        List<PlatformAuditLogEntry> auditEntries =
            await ReadAuditEntriesAsync(readScope, tenantId, "platform.organization.created");
        auditEntries.Should().ContainSingle();
        auditEntries[0].TargetType.Should().Be("Tenant");
        auditEntries[0].TargetId.Should().Be(tenantId.ToString());
    }

    /// <summary>
    /// Items 2/3 — the core regression proof. Replicates exactly what the fixed endpoint does
    /// (<see cref="IUnitOfWork.BeginTransactionAsync"/>, dispatch the mutation, add the audit record,
    /// save) but deliberately never calls <see cref="IUnitOfWorkTransaction.CommitAsync"/> — standing in
    /// for a process crash/failure in the window between the audit save succeeding locally and the
    /// caller reaching the commit call. Before the H-01 fix this window did not exist inside a shared
    /// transaction at all: the mutation's own <c>SaveChangesAsync</c> committed unconditionally as soon
    /// as the command handler returned, regardless of what happened afterward. Proves that with the fix,
    /// disposing the transaction without committing rolls back both the mutation and the audit record
    /// together — neither is left half-persisted.
    /// </summary>
    [Fact]
    public async Task Disposing_The_Shared_Transaction_Without_Committing_Leaves_Neither_The_Mutation_Nor_The_Audit_Persisted()
    {
        Guid tenantId = await ProvisionOrganizationEndToEndAsync(_factory, "rollback");

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
            IPlatformAuditLogRepository auditLog = scope.ServiceProvider.GetRequiredService<IPlatformAuditLogRepository>();
            IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

            await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None);

            await sender.Send(new UpdateOrganizationCommand(tenantId, "Should Never Be Persisted", null), CancellationToken.None);

            auditLog.Add(PlatformAuditLogEntry.Record(
                clock.UtcNow,
                actorPlatformUserId: null,
                action: "platform.organization.updated",
                targetType: "Tenant",
                targetId: tenantId.ToString(),
                tenantId: tenantId));
            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            // Deliberately no transaction.CommitAsync() call here — the `await using` disposal below
            // rolls back everything written through this scope's connection since BeginTransactionAsync,
            // simulating a crash in the window before commit.
        }

        using IServiceScope readScope = _factory.Services.CreateScope();
        Tenant tenant = (await readScope.ServiceProvider.GetRequiredService<ITenantRepository>()
            .GetByIdAsync(tenantId, CancellationToken.None))!;
        tenant.Name.Should().NotBe("Should Never Be Persisted");

        List<PlatformAuditLogEntry> auditEntries =
            await ReadAuditEntriesAsync(readScope, tenantId, "platform.organization.updated");
        auditEntries.Should().BeEmpty();
    }
}
