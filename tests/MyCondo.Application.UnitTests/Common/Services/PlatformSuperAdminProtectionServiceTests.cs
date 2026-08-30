using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// Unit tests for <see cref="PlatformSuperAdminProtectionService"/> (mycondo-docs ADR-035) — the
/// Platform-scope analogue of <see cref="TenantAdminProtectionServiceTests"/>, guarding the "SuperAdmin"
/// Platform role.
/// </summary>
public class PlatformSuperAdminProtectionServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPlatformRoleRepository _platformRoles = Substitute.For<IPlatformRoleRepository>();
    private readonly IPlatformUserRoleAssignmentRepository _assignments = Substitute.For<IPlatformUserRoleAssignmentRepository>();

    private PlatformSuperAdminProtectionService CreateService() => new(_platformRoles, _assignments);

    private static PlatformRole SuperAdminRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "SuperAdmin", "Full platform access", Now);

    private static PlatformRole OtherRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "Support", "Limited access", Now);

    // --- IsSuperAdmin -------------------------------------------------------------------------------

    [Fact]
    public void IsSuperAdmin_True_For_Role_Named_SuperAdmin()
    {
        CreateService().IsSuperAdmin(SuperAdminRole()).Should().BeTrue();
    }

    [Fact]
    public void IsSuperAdmin_False_For_Differently_Named_Role()
    {
        CreateService().IsSuperAdmin(OtherRole()).Should().BeFalse();
    }

    // --- TargetIsSuperAdminAsync ---------------------------------------------------------------------

    [Fact]
    public async Task TargetIsSuperAdminAsync_True_When_Target_Holds_The_SuperAdmin_Role()
    {
        PlatformUserId targetId = PlatformUserId.New();
        PlatformRole superAdminRole = SuperAdminRole();
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns(superAdminRole);
        _assignments.ExistsAsync(targetId, superAdminRole.Id, Arg.Any<CancellationToken>()).Returns(true);

        bool result = await CreateService().TargetIsSuperAdminAsync(targetId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TargetIsSuperAdminAsync_False_When_Target_Does_Not_Hold_The_Role()
    {
        PlatformUserId targetId = PlatformUserId.New();
        PlatformRole superAdminRole = SuperAdminRole();
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns(superAdminRole);
        _assignments.ExistsAsync(targetId, superAdminRole.Id, Arg.Any<CancellationToken>()).Returns(false);

        bool result = await CreateService().TargetIsSuperAdminAsync(targetId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TargetIsSuperAdminAsync_False_When_The_SuperAdmin_Role_Does_Not_Exist()
    {
        PlatformUserId targetId = PlatformUserId.New();
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns((PlatformRole?)null);

        bool result = await CreateService().TargetIsSuperAdminAsync(targetId, CancellationToken.None);

        result.Should().BeFalse();
    }

    // --- EnsureCanMutateAdminTarget -------------------------------------------------------------------

    [Fact]
    public void EnsureCanMutateAdminTarget_Throws_When_Actor_Targets_Own_Account_Even_With_Permission()
    {
        Guid actorId = Guid.NewGuid();

        Action act = () => CreateService().EnsureCanMutateAdminTarget(actorId, actorId, actorCanManageSuperAdmins: true);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureCanMutateAdminTarget_Throws_When_Actor_Lacks_ManageSuperAdmins()
    {
        Action act = () => CreateService().EnsureCanMutateAdminTarget(
            Guid.NewGuid(), Guid.NewGuid(), actorCanManageSuperAdmins: false);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureCanMutateAdminTarget_Succeeds_For_Different_Target_With_Permission()
    {
        Action act = () => CreateService().EnsureCanMutateAdminTarget(
            Guid.NewGuid(), Guid.NewGuid(), actorCanManageSuperAdmins: true);

        act.Should().NotThrow();
    }

    // --- EnsureCanEditAdminTarget ----------------------------------------------------------------------

    [Fact]
    public void EnsureCanEditAdminTarget_Allows_Self_Edit_Without_Permission()
    {
        Guid actorId = Guid.NewGuid();

        Action act = () => CreateService().EnsureCanEditAdminTarget(actorId, actorId, actorCanManageSuperAdmins: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEditAdminTarget_Throws_When_Editing_Another_Admin_Without_Permission()
    {
        Action act = () => CreateService().EnsureCanEditAdminTarget(
            Guid.NewGuid(), Guid.NewGuid(), actorCanManageSuperAdmins: false);

        act.Should().Throw<ForbiddenException>();
    }

    // --- EnsureNotLastActiveSuperAdminAsync -------------------------------------------------------------

    [Fact]
    public async Task EnsureNotLastActiveSuperAdminAsync_Throws_When_Only_One_Active_Holder_Remains()
    {
        PlatformRole superAdminRole = SuperAdminRole();
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns(superAdminRole);
        _assignments.LockAndCountActiveSuperAdminHoldersAsync(superAdminRole.Id, Arg.Any<CancellationToken>()).Returns(1);

        Func<Task> act = () => CreateService().EnsureNotLastActiveSuperAdminAsync(CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task EnsureNotLastActiveSuperAdminAsync_Succeeds_When_Another_Active_Holder_Remains()
    {
        PlatformRole superAdminRole = SuperAdminRole();
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns(superAdminRole);
        _assignments.LockAndCountActiveSuperAdminHoldersAsync(superAdminRole.Id, Arg.Any<CancellationToken>()).Returns(2);

        Func<Task> act = () => CreateService().EnsureNotLastActiveSuperAdminAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureNotLastActiveSuperAdminAsync_Does_Nothing_When_The_SuperAdmin_Role_Does_Not_Exist()
    {
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns((PlatformRole?)null);

        Func<Task> act = () => CreateService().EnsureNotLastActiveSuperAdminAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _assignments.DidNotReceive().LockAndCountActiveSuperAdminHoldersAsync(
            Arg.Any<PlatformRoleId>(), Arg.Any<CancellationToken>());
    }
}
