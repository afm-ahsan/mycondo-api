using AwesomeAssertions;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Domain.UnitTests.Features.Security.Guests;

public class GuestProfileTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Register_Trims_Fields()
    {
        GuestProfile guest = GuestProfile.Register(
            TenantId, "  Jane Doe  ", " 01700000000 ", " NID ", " 1234567890 ", Now);

        guest.FullName.Should().Be("Jane Doe");
        guest.Phone.Should().Be("+8801700000000");
        guest.IdentityDocumentType.Should().Be("NID");
        guest.IdentityDocumentNumber.Should().Be("1234567890");
        guest.IsBlocked.Should().BeFalse();
        guest.Version.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Throws_When_FullName_Is_Blank(string fullName)
    {
        Action act = () => GuestProfile.Register(TenantId, fullName, "017", null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Throws_When_Phone_Is_Blank(string phone)
    {
        Action act = () => GuestProfile.Register(TenantId, "Jane Doe", phone, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Block_Sets_IsBlocked_And_Reason()
    {
        GuestProfile guest = GuestProfile.Register(TenantId, "Jane Doe", "01712345678", null, null, Now);

        guest.Block("Reported theft");

        guest.IsBlocked.Should().BeTrue();
        guest.BlockReason.Should().Be("Reported theft");
        guest.Version.Should().Be(2);
    }

    [Fact]
    public void Unblock_Clears_IsBlocked_And_Reason()
    {
        GuestProfile guest = GuestProfile.Register(TenantId, "Jane Doe", "01712345678", null, null, Now);
        guest.Block("Reported theft");

        guest.Unblock();

        guest.IsBlocked.Should().BeFalse();
        guest.BlockReason.Should().BeNull();
        guest.Version.Should().Be(3);
    }
}
