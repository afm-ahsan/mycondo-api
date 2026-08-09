using AwesomeAssertions;
using Microsoft.Extensions.Options;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Infrastructure.Identity;
using MyCondo.Infrastructure.Persistence.Seeding.Extensions;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Persistence.Seeding;

public class UserSeedExtensionsTests
{
    // Low cost factors on purpose, same rationale as Argon2idPasswordHasherTests — this exercises the
    // real hashing workflow, not production tuning.
    private static Argon2idPasswordHasher RealHasher() =>
        new(Options.Create(new Argon2Settings { MemoryKb = 8192, Iterations = 1, Parallelism = 1 }));

    private static IClock FixedClock(DateTimeOffset now)
    {
        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    [Fact]
    public async Task EnsureUserAsync_Creates_User_With_Hashed_Password_When_Missing()
    {
        IUserRepository users = Substitute.For<IUserRepository>();
        Guid tenantId = Guid.NewGuid();
        users.GetByEmailAsync(tenantId, "sadmin@mycondo.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        User? added = null;
        users.When(u => u.Add(Arg.Any<User>())).Do(call => added = call.Arg<User>());

        Argon2idPasswordHasher hasher = RealHasher();

        (User user, bool created) = await users.EnsureUserAsync(
            tenantId, "SAdmin@mycondo.com", "SuperAdmin", "SAdmin@1357#", hasher, FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        created.Should().BeTrue();
        added.Should().BeSameAs(user);
        user.Email.Should().Be("sadmin@mycondo.com");
        user.PasswordHash.Should().NotBe("SAdmin@1357#");
        user.PasswordHash.Should().StartWith("$argon2id$");
        hasher.Verify("SAdmin@1357#", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureUserAsync_Returns_Existing_User_Without_Rehashing_Or_Readding()
    {
        IUserRepository users = Substitute.For<IUserRepository>();
        Guid tenantId = Guid.NewGuid();
        User existing = User.Register(tenantId, "sadmin@mycondo.com", "already-hashed", "SuperAdmin", null, DateTimeOffset.UtcNow);
        users.GetByEmailAsync(tenantId, "sadmin@mycondo.com", Arg.Any<CancellationToken>()).Returns(existing);

        IPasswordHasher hasher = Substitute.For<IPasswordHasher>();

        (User user, bool created) = await users.EnsureUserAsync(
            tenantId, "sadmin@mycondo.com", "SuperAdmin", "SAdmin@1357#", hasher, FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        created.Should().BeFalse();
        user.Should().BeSameAs(existing);
        user.PasswordHash.Should().Be("already-hashed");
        hasher.DidNotReceive().Hash(Arg.Any<string>());
        users.DidNotReceive().Add(Arg.Any<User>());
    }
}
