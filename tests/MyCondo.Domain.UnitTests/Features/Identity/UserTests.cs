using AwesomeAssertions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Identity.Users.Events;
using MyCondo.Domain.Features.Identity.Users.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Identity;

public class UserTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Register_Normalizes_Email_And_Trims_Name()
    {
        User user = User.Register(
            TenantId, "  Someone@Example.com  ", "hash", "  Jane Doe  ", null, Now);

        user.Email.Should().Be("someone@example.com");
        user.FullName.Should().Be("Jane Doe");
        user.Status.Should().Be(UserStatus.Active);
        user.EmailConfirmed.Should().BeFalse();
        user.Version.Should().Be(1);
    }

    [Fact]
    public void Register_Raises_UserRegisteredEvent()
    {
        User user = User.Register(TenantId, "a@b.com", "hash", "A B", null, Now);

        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Throws_When_Email_Is_Blank(string email)
    {
        Action act = () => User.Register(TenantId, email, "hash", "A B", null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_Throws_When_TenantId_Is_Empty()
    {
        Action act = () => User.Register(Guid.Empty, "a@b.com", "hash", "A B", null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangePassword_Bumps_Version_When_Hash_Differs()
    {
        User user = User.Register(TenantId, "a@b.com", "hash-1", "A B", null, Now);

        user.ChangePassword("hash-2", Now.AddMinutes(1));

        user.PasswordHash.Should().Be("hash-2");
        user.Version.Should().Be(2);
    }

    [Fact]
    public void ChangePassword_Is_A_NoOp_When_Hash_Is_Unchanged()
    {
        User user = User.Register(TenantId, "a@b.com", "hash-1", "A B", null, Now);

        user.ChangePassword("hash-1", Now.AddMinutes(1));

        user.Version.Should().Be(1);
    }

    [Fact]
    public void Deactivate_Throws_When_Already_Inactive()
    {
        User user = User.Register(TenantId, "a@b.com", "hash", "A B", null, Now);
        user.Deactivate(Now);

        Action act = () => user.Deactivate(Now);

        act.Should().Throw<UserAlreadyDeactivatedException>();
    }

    [Fact]
    public void Activate_Is_A_NoOp_When_Already_Active()
    {
        User user = User.Register(TenantId, "a@b.com", "hash", "A B", null, Now);

        user.Activate(Now);

        user.Version.Should().Be(1);
    }
}
