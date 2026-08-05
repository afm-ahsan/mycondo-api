using AwesomeAssertions;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Domain.UnitTests.Features.Security.Vehicles;

public class VehicleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Register_Normalizes_RegistrationNumber()
    {
        Vehicle vehicle = Vehicle.Register(
            TenantId, "  dha metro-ga 12 3456  ", VehicleType.Car, "Toyota", "Corolla", "White",
            VehicleOwnershipCategory.Resident, null, Now);

        vehicle.RegistrationNumber.Should().Be("DHAMETRO-GA123456");
    }

    [Theory]
    [InlineData("DHA METRO GA 123456", "DHAMETROGA123456")]
    [InlineData("dha-metro-ga-123456", "DHA-METRO-GA-123456")]
    public void NormalizeRegistrationNumber_Is_Case_And_Whitespace_Insensitive(string input, string expected)
    {
        Vehicle.NormalizeRegistrationNumber(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Throws_When_RegistrationNumber_Is_Blank(string registrationNumber)
    {
        Action act = () => Vehicle.Register(
            TenantId, registrationNumber, VehicleType.Car, null, null, null, VehicleOwnershipCategory.Resident,
            null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Block_Sets_IsBlocked_And_Reason()
    {
        Vehicle vehicle = Vehicle.Register(
            TenantId, "ABC123", VehicleType.Car, null, null, null, VehicleOwnershipCategory.Resident, null, Now);

        vehicle.Block("Reported stolen");

        vehicle.IsBlocked.Should().BeTrue();
        vehicle.BlockReason.Should().Be("Reported stolen");
    }

    [Fact]
    public void Unblock_Clears_IsBlocked_And_Reason()
    {
        Vehicle vehicle = Vehicle.Register(
            TenantId, "ABC123", VehicleType.Car, null, null, null, VehicleOwnershipCategory.Resident, null, Now);
        vehicle.Block("Reported stolen");

        vehicle.Unblock();

        vehicle.IsBlocked.Should().BeFalse();
        vehicle.BlockReason.Should().BeNull();
    }

    [Fact]
    public void Deactivate_Sets_DeletedAtUtc()
    {
        Vehicle vehicle = Vehicle.Register(
            TenantId, "ABC123", VehicleType.Car, null, null, null, VehicleOwnershipCategory.Resident, null, Now);

        vehicle.Deactivate(Now.AddDays(1), Guid.NewGuid());

        vehicle.DeletedAtUtc.Should().Be(Now.AddDays(1));
    }
}
