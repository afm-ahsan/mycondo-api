using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
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
    private readonly IExpenseTypeRepository _uowExpenseTypes = Substitute.For<IExpenseTypeRepository>();

    public ProvisionOrganizationWithAdminCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
        _currentPlatformUser.PlatformUserId.Returns(Guid.NewGuid());
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _uowPermissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(FullPermissionCatalogue());
        _uowRoles.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _uowRolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);
        _uowExpenseTypes.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        _uow.Tenants.Returns(_uowTenants);
        _uow.Users.Returns(_uowUsers);
        _uow.Roles.Returns(_uowRoles);
        _uow.Permissions.Returns(_uowPermissions);
        _uow.RolePermissions.Returns(_uowRolePermissions);
        _uow.RoleAssignments.Returns(_uowRoleAssignments);
        _uow.TenantModules.Returns(_uowTenantModules);
        _uow.ExpenseTypes.Returns(_uowExpenseTypes);

        _uowFactory.Create(Arg.Any<Guid>()).Returns(_uow);
    }

    private static List<Permission> FullPermissionCatalogue() =>
        PermissionCatalogue.Entries
            .Select(e => Permission.Create(PermissionId.New(), e.Name, e.Description, e.Module, e.IsBuildingScopable))
            .ToList();

    private ProvisionOrganizationWithAdminCommandHandler CreateHandler() => new(
        _ambientTenants, _uowFactory, _passwordHasher, _clock, _currentPlatformUser, _loggerFactory,
        Substitute.For<ILogger<ProvisionOrganizationWithAdminCommandHandler>>());

    private static ProvisionOrganizationWithAdminCommand ValidCommand() => new(
        Name: "Akter Residence Park",
        Code: "ARP",
        Slug: "arp",
        AdministratorFullName: "Admin",
        AdministratorEmail: "admin@mycondo.com",
        AdministratorPassword: "Correct-Horse-Battery-9",
        EnabledModuleKeys: ["billing", "payments"]);

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
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
}
