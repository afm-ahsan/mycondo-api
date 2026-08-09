using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Infrastructure.Persistence.Seeding.Extensions;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Persistence.Seeding;

public class RoleSeedExtensionsTests
{
    private static IClock FixedClock(DateTimeOffset now)
    {
        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    private static User AnyUser(Guid tenantId) =>
        User.Register(tenantId, "user@mycondo.com", "hash", "Any User", null, DateTimeOffset.UtcNow);

    [Fact]
    public async Task EnsureSuperAdminAsync_Bootstraps_When_SuperAdmin_Role_Does_Not_Exist()
    {
        Guid tenantId = Guid.NewGuid();
        User user = AnyUser(tenantId);

        IRoleRepository roles = Substitute.For<IRoleRepository>();
        roles.GetByNameAsync(tenantId, "SuperAdmin", Arg.Any<CancellationToken>()).Returns((Role?)null);
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();
        ISuperAdminBootstrapper bootstrapper = Substitute.For<ISuperAdminBootstrapper>();

        await roles.EnsureSuperAdminAsync(roleAssignments, bootstrapper, tenantId, user, FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        await bootstrapper.Received(1).BootstrapAsync(tenantId, user, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await roleAssignments.DidNotReceive().ExistsAsync(
            Arg.Any<Guid>(), Arg.Any<UserId>(), Arg.Any<RoleId>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureSuperAdminAsync_Assigns_Existing_Role_When_User_Not_Yet_Assigned()
    {
        Guid tenantId = Guid.NewGuid();
        User user = AnyUser(tenantId);
        Role existingRole = Role.CreateSystem(RoleId.New(), tenantId, "SuperAdmin", "desc", DateTimeOffset.UtcNow);

        IRoleRepository roles = Substitute.For<IRoleRepository>();
        roles.GetByNameAsync(tenantId, "SuperAdmin", Arg.Any<CancellationToken>()).Returns(existingRole);
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();
        roleAssignments.ExistsAsync(tenantId, user.Id, existingRole.Id, null, Arg.Any<CancellationToken>()).Returns(false);
        ISuperAdminBootstrapper bootstrapper = Substitute.For<ISuperAdminBootstrapper>();

        await roles.EnsureSuperAdminAsync(roleAssignments, bootstrapper, tenantId, user, FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        await bootstrapper.DidNotReceive().BootstrapAsync(Arg.Any<Guid>(), Arg.Any<User>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        roleAssignments.Received(1).Add(Arg.Is<RoleAssignment>(a => a.RoleId == existingRole.Id && a.UserId == user.Id));
    }

    [Fact]
    public async Task EnsureSuperAdminAsync_NoOps_When_Role_And_Assignment_Already_Exist()
    {
        Guid tenantId = Guid.NewGuid();
        User user = AnyUser(tenantId);
        Role existingRole = Role.CreateSystem(RoleId.New(), tenantId, "SuperAdmin", "desc", DateTimeOffset.UtcNow);

        IRoleRepository roles = Substitute.For<IRoleRepository>();
        roles.GetByNameAsync(tenantId, "SuperAdmin", Arg.Any<CancellationToken>()).Returns(existingRole);
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();
        roleAssignments.ExistsAsync(tenantId, user.Id, existingRole.Id, null, Arg.Any<CancellationToken>()).Returns(true);
        ISuperAdminBootstrapper bootstrapper = Substitute.For<ISuperAdminBootstrapper>();

        await roles.EnsureSuperAdminAsync(roleAssignments, bootstrapper, tenantId, user, FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        await bootstrapper.DidNotReceive().BootstrapAsync(Arg.Any<Guid>(), Arg.Any<User>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        roleAssignments.DidNotReceive().Add(Arg.Any<RoleAssignment>());
    }

    [Fact]
    public async Task EnsureDefaultRoleCatalogueAsync_Seeds_When_Probe_Role_Missing()
    {
        Guid tenantId = Guid.NewGuid();
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        roles.GetByNameAsync(tenantId, "BuildingAdmin", Arg.Any<CancellationToken>()).Returns((Role?)null);
        IDefaultRoleCatalogueSeeder seeder = Substitute.For<IDefaultRoleCatalogueSeeder>();

        await roles.EnsureDefaultRoleCatalogueAsync(seeder, tenantId, "BuildingAdmin", FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        await seeder.Received(1).SeedAsync(tenantId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureDefaultRoleCatalogueAsync_NoOps_When_Probe_Role_Already_Exists()
    {
        Guid tenantId = Guid.NewGuid();
        Role existing = Role.CreateCustom(tenantId, "BuildingAdmin", "desc", DateTimeOffset.UtcNow);
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        roles.GetByNameAsync(tenantId, "BuildingAdmin", Arg.Any<CancellationToken>()).Returns(existing);
        IDefaultRoleCatalogueSeeder seeder = Substitute.For<IDefaultRoleCatalogueSeeder>();

        await roles.EnsureDefaultRoleCatalogueAsync(seeder, tenantId, "BuildingAdmin", FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        await seeder.DidNotReceive().SeedAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureRoleAssignmentAsync_Throws_When_Named_Role_Does_Not_Exist()
    {
        Guid tenantId = Guid.NewGuid();
        User user = AnyUser(tenantId);
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        roles.GetByNameAsync(tenantId, "Owner", Arg.Any<CancellationToken>()).Returns((Role?)null);
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();

        Func<Task> act = () => roles.EnsureRoleAssignmentAsync(roleAssignments, tenantId, user, "Owner", FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Owner*");
    }

    [Fact]
    public async Task EnsureRoleAssignmentAsync_Grants_Role_Once_And_Is_Idempotent_On_Second_Call()
    {
        Guid tenantId = Guid.NewGuid();
        User user = AnyUser(tenantId);
        Role owner = Role.CreateCustom(tenantId, "Owner", "desc", DateTimeOffset.UtcNow);

        IRoleRepository roles = Substitute.For<IRoleRepository>();
        roles.GetByNameAsync(tenantId, "Owner", Arg.Any<CancellationToken>()).Returns(owner);
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();

        bool assigned = false;
        roleAssignments.ExistsAsync(tenantId, user.Id, owner.Id, null, Arg.Any<CancellationToken>())
            .Returns(_ => assigned);
        roleAssignments.When(r => r.Add(Arg.Any<RoleAssignment>())).Do(_ => assigned = true);

        await roles.EnsureRoleAssignmentAsync(roleAssignments, tenantId, user, "Owner", FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);
        await roles.EnsureRoleAssignmentAsync(roleAssignments, tenantId, user, "Owner", FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        roleAssignments.Received(1).Add(Arg.Any<RoleAssignment>());
    }
}
