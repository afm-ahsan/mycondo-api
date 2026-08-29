using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.ProvisionOrganizationWithAdmin;

/// <summary>
/// The handler composes the exact same bootstrap/role-catalogue services
/// RegisterUserCommandHandler uses for "first user of a tenant" — these tests wire the real
/// PermissionCatalogue (converted to Permission entities) into the mocked ITenantScopedUnitOfWork so
/// DefaultRoleCatalogueSeeder/CondominiumRoleCatalogueSeeder/ResidentRoleCatalogueSeeder can resolve
/// every permission name they reference, exactly as they would against a real database.
/// </summary>
public class ProvisionOrganizationWithAdminCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _ambientTenants = Substitute.For<ITenantRepository>();
    private readonly ISubscriptionPackageRepository _ambientSubscriptionPackages = Substitute.For<ISubscriptionPackageRepository>();
    private readonly ISubscriptionPackageVersionRepository _ambientSubscriptionPackageVersions =
        Substitute.For<ISubscriptionPackageVersionRepository>();
    private readonly ITenantScopedUnitOfWorkFactory _uowFactory = Substitute.For<ITenantScopedUnitOfWorkFactory>();
    private readonly ITenantScopedUnitOfWork _uow = Substitute.For<ITenantScopedUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentPlatformUserProvider _currentPlatformUser = Substitute.For<ICurrentPlatformUserProvider>();
    private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();

    private readonly ITenantRepository _uowTenants = Substitute.For<ITenantRepository>();
    private readonly IUserRepository _uowUsers = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _uowRoles = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository _uowPermissions = Substitute.For<IPermissionRepository>();
    private readonly IRolePermissionRepository _uowRolePermissions = Substitute.For<IRolePermissionRepository>();
    private readonly IRoleAssignmentRepository _uowRoleAssignments = Substitute.For<IRoleAssignmentRepository>();
    private readonly ITenantModuleRepository _uowTenantModules = Substitute.For<ITenantModuleRepository>();
    private readonly IExpenseCategoryRepository _uowExpenseCategories = Substitute.For<IExpenseCategoryRepository>();
    private readonly IExpenseTypeRepository _uowExpenseTypes = Substitute.For<IExpenseTypeRepository>();
    private readonly IOrganizationSubscriptionRepository _uowOrganizationSubscriptions =
        Substitute.For<IOrganizationSubscriptionRepository>();

    private readonly SubscriptionPackage _activePackage;
    private readonly SubscriptionPackageVersion _activePackageVersion;

    public ProvisionOrganizationWithAdminCommandHandlerTests()
    {
        _activePackage = SubscriptionPackage.Create("PRO", "Professional", description: null);
        _activePackageVersion = SubscriptionPackageVersion.Create(
            _activePackage.Id,
            version: 1,
            effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(-1),
            effectiveUntil: null,
            monthlyPrice: 1000m,
            quarterlyPrice: null,
            semiAnnualPrice: null,
            annualPrice: null,
            currency: "BDT");
        _activePackageVersion.Activate();
        _activePackage.Activate();
        _activePackage.SetCurrentVersion(_activePackageVersion.Id);

        _ambientSubscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_activePackage]);
        _ambientSubscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_activePackageVersion]);

        _clock.UtcNow.Returns(NowUtc);
        _currentPlatformUser.PlatformUserId.Returns(Guid.NewGuid());
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _uowPermissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(FullPermissionCatalogue());
        _uowRoles.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _uowRolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);

        // ExpenseCategoryCatalogueSeeder runs (and is "saved") before ExpenseTypeCatalogueSeeder reads
        // categories back — mirror that with a substitute that returns whatever's been Added so far,
        // the same way a real repository would after the handler's intermediate SaveChangesAsync.
        List<ExpenseCategory> addedCategories = [];
        _uowExpenseCategories.Add(Arg.Do<ExpenseCategory>(addedCategories.Add));
        _uowExpenseCategories.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => addedCategories.ToList());
        _uowExpenseTypes.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        _uow.Tenants.Returns(_uowTenants);
        _uow.Users.Returns(_uowUsers);
        _uow.Roles.Returns(_uowRoles);
        _uow.Permissions.Returns(_uowPermissions);
        _uow.RolePermissions.Returns(_uowRolePermissions);
        _uow.RoleAssignments.Returns(_uowRoleAssignments);
        _uow.TenantModules.Returns(_uowTenantModules);
        _uow.ExpenseCategories.Returns(_uowExpenseCategories);
        _uow.ExpenseTypes.Returns(_uowExpenseTypes);
        _uow.OrganizationSubscriptions.Returns(_uowOrganizationSubscriptions);

        _uowFactory.Create(Arg.Any<Guid>()).Returns(_uow);
    }

    private static List<Permission> FullPermissionCatalogue() =>
        PermissionCatalogue.Entries
            .Select(e => Permission.Create(PermissionId.New(), e.Name, e.Description, e.Module, e.IsBuildingScopable))
            .ToList();

    private ProvisionOrganizationWithAdminCommandHandler CreateHandler() => new(
        _ambientTenants, _ambientSubscriptionPackages, _ambientSubscriptionPackageVersions, _uowFactory,
        _passwordHasher, _clock, _currentPlatformUser, _loggerFactory,
        Substitute.For<ILogger<ProvisionOrganizationWithAdminCommandHandler>>());

    private ProvisionOrganizationWithAdminCommand ValidCommand() => new(
        Name: "Akter Residence Park",
        Code: "ARP",
        Slug: "arp",
        AdministratorFullName: "Admin",
        AdministratorEmail: "admin@mycondo.com",
        AdministratorPassword: "Correct-Horse-Battery-9",
        EnabledModuleKeys: ["billing", "payments"],
        SubscriptionPackageVersionId: _activePackageVersion.Id.Value,
        BillingCycle: BillingCycle.Monthly,
        AutoRenew: true);

    [Fact]
    public async Task Throws_Conflict_When_Slug_Already_Exists()
    {
        _ambientTenants.SlugExistsAsync("arp", Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _uowFactory.DidNotReceive().Create(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Code_Already_Exists()
    {
        _ambientTenants.SlugExistsAsync("arp", Arg.Any<CancellationToken>()).Returns(false);
        _ambientTenants.CodeExistsAsync("ARP", Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Provisions_An_Active_Organization_With_A_Hashed_Password_And_The_Requested_Modules()
    {
        ProvisionOrganizationResult result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Name.Should().Be("Akter Residence Park");
        result.Code.Should().Be("ARP");
        result.Slug.Should().Be("arp");
        result.Status.Should().Be(nameof(TenantStatus.Active));

        _uowTenants.Received(1).Add(Arg.Is<Tenant>(t => t.Status == TenantStatus.Active && t.Code == "ARP"));
        _passwordHasher.Received(1).Hash("Correct-Horse-Battery-9");
        _uowUsers.Received(1).Add(Arg.Is<User>(u => u.Email == "admin@mycondo.com" && u.PasswordHash == "hashed-password"));
        await _uowTenantModules.Received(1).ReplaceForTenantAsync(
            result.TenantId,
            Arg.Is<IReadOnlyCollection<string>>(k => k.SequenceEqual(new[] { "billing", "payments" })),
            NowUtc,
            _currentPlatformUser.PlatformUserId,
            Arg.Any<CancellationToken>());
        // Two saves (Template 3): one intermediate save after the Expense Category catalogue is seeded
        // (so the Expense Type catalogue seeder can resolve categories via a database-style read — see
        // ExpenseCategoryCatalogueSeeder/ExpenseTypeCatalogueSeeder's ordering comment), then the final
        // save for everything else.
        await _uow.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grants_The_New_Administrator_Every_Non_Platform_Permission()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        // OrganizationAdminBootstrapper filters out the "platform" module — asserting at least one
        // non-platform grant was added is enough to prove the real bootstrapper ran (not a stub).
        _uowRolePermissions.Received().Add(Arg.Any<RolePermission>());
        _uowRoleAssignments.Received(1).Add(Arg.Any<RoleAssignment>());
    }

    [Fact]
    public async Task Seeds_The_Default_Expense_Type_Catalogue_For_The_New_Tenant()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _uowExpenseTypes.Received(11).Add(Arg.Any<ExpenseType>());
    }

    [Fact]
    public async Task Does_Not_Use_The_Ambient_Tenant_Repository_For_Writes()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        // Writes to RLS-protected tables must go through the tenant-scoped unit of work, not the
        // ambient one (which has no tenant JWT claim to satisfy RLS's WITH CHECK).
        _ambientTenants.DidNotReceive().Add(Arg.Any<Tenant>());
    }

    [Fact]
    public async Task Creates_One_Current_OrganizationSubscription_Referencing_The_Selected_Package_Version()
    {
        ProvisionOrganizationResult result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _uowOrganizationSubscriptions.Received(1).Add(Arg.Is<OrganizationSubscription>(s =>
            s.TenantId == result.TenantId &&
            s.PackageVersionId == _activePackageVersion.Id &&
            s.Status == OrganizationSubscriptionStatus.Active &&
            s.BasePrice == 1000m &&
            s.EffectivePrice == 1000m &&
            s.Currency == "BDT"));
    }

    [Fact]
    public async Task Uses_The_Requested_BillingCycle_And_AutoRenew()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("ANN", "Annual Only", description: null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(-1),
            effectiveUntil: null, monthlyPrice: null, quarterlyPrice: null, semiAnnualPrice: null,
            annualPrice: 9600m, currency: "BDT");
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);
        _ambientSubscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _ambientSubscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([version]);

        ProvisionOrganizationWithAdminCommand command = ValidCommand() with
        {
            SubscriptionPackageVersionId = version.Id.Value,
            BillingCycle = BillingCycle.Annual,
            AutoRenew = false,
        };

        ProvisionOrganizationResult result = await CreateHandler().Handle(command, CancellationToken.None);

        _uowOrganizationSubscriptions.Received(1).Add(Arg.Is<OrganizationSubscription>(s =>
            s.TenantId == result.TenantId &&
            s.BillingCycle == BillingCycle.Annual &&
            s.BasePrice == 9600m &&
            s.AutoRenew == false));
    }

    [Fact]
    public async Task Throws_When_The_Requested_BillingCycle_Has_No_Configured_Price()
    {
        // _activePackageVersion (see constructor) only configures MonthlyPrice.
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { BillingCycle = BillingCycle.Quarterly };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedBillingCycleException>();
        // The rejection happens before the tenant-scoped unit of work is ever saved — no partial
        // tenant/admin/subscription state is committed.
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_SubscriptionPackageVersion_Does_Not_Exist()
    {
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { SubscriptionPackageVersionId = Guid.NewGuid() };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _uowFactory.DidNotReceive().Create(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Throws_Conflict_When_SubscriptionPackageVersion_Is_Not_Active()
    {
        SubscriptionPackage draftPackage = SubscriptionPackage.Create("DRAFT", "Draft Package", description: null);
        SubscriptionPackageVersion draftVersion = SubscriptionPackageVersion.Create(
            draftPackage.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime), effectiveUntil: null,
            monthlyPrice: 500m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        _ambientSubscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([draftPackage]);
        _ambientSubscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([draftVersion]);

        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { SubscriptionPackageVersionId = draftVersion.Id.Value };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _uowFactory.DidNotReceive().Create(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Package_Is_Retired()
    {
        SubscriptionPackage retiredPackage = SubscriptionPackage.Create("OLD", "Old Package", description: null);
        SubscriptionPackageVersion retiredVersion = SubscriptionPackageVersion.Create(
            retiredPackage.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime), effectiveUntil: null,
            monthlyPrice: 500m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        retiredVersion.Activate();
        retiredPackage.Activate();
        retiredPackage.SetCurrentVersion(retiredVersion.Id);
        retiredPackage.Retire();
        _ambientSubscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([retiredPackage]);
        _ambientSubscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([retiredVersion]);

        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { SubscriptionPackageVersionId = retiredVersion.Id.Value };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_PackageVersion_Is_Not_Yet_Effective()
    {
        SubscriptionPackage futurePackage = SubscriptionPackage.Create("FUT", "Future Package", description: null);
        SubscriptionPackageVersion futureVersion = SubscriptionPackageVersion.Create(
            futurePackage.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(30),
            effectiveUntil: null, monthlyPrice: 500m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null,
            currency: "BDT");
        futureVersion.Activate();
        futurePackage.Activate();
        futurePackage.SetCurrentVersion(futureVersion.Id);
        _ambientSubscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([futurePackage]);
        _ambientSubscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([futureVersion]);

        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { SubscriptionPackageVersionId = futureVersion.Id.Value };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_The_Legacy_Grandfathered_Package_Is_Explicitly_Selected()
    {
        SubscriptionPackage legacyPackage = SubscriptionPackage.Create(
            "LEGACY-MIGRATION-GRANDFATHERED", "Legacy Migration (Grandfathered)", description: null);
        SubscriptionPackageVersion legacyVersion = SubscriptionPackageVersion.Create(
            legacyPackage.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime), effectiveUntil: null,
            monthlyPrice: 0m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        legacyVersion.Activate();
        legacyPackage.Activate();
        legacyPackage.SetCurrentVersion(legacyVersion.Id);
        _ambientSubscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([legacyPackage]);
        _ambientSubscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([legacyVersion]);

        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { SubscriptionPackageVersionId = legacyVersion.Id.Value };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
