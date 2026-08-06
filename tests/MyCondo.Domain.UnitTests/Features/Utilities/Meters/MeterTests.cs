using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Meters.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Utilities.Meters;

public class MeterTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();

    private static Meter InstallMeter() =>
        Meter.Install(TenantId, BuildingId, UtilityType.Electricity, "MTR-001", Now);

    [Fact]
    public void Install_Starts_Active_With_Version_One()
    {
        Meter meter = InstallMeter();

        meter.Status.Should().Be(MeterStatus.Active);
        meter.ReplacesMeterId.Should().BeNull();
        meter.Version.Should().Be(1);
    }

    [Fact]
    public void MarkFaulty_Sets_Faulty_Status()
    {
        Meter meter = InstallMeter();

        meter.MarkFaulty("Not registering readings");

        meter.Status.Should().Be(MeterStatus.Faulty);
        meter.Version.Should().Be(2);
    }

    [Fact]
    public void MarkFaulty_Throws_When_Not_Active()
    {
        Meter meter = InstallMeter();
        meter.MarkFaulty("First fault");

        Action act = () => meter.MarkFaulty("Second attempt");

        act.Should().Throw<MeterInvalidStateTransitionException>();
    }

    [Fact]
    public void Reactivate_From_Faulty_Sets_Active()
    {
        Meter meter = InstallMeter();
        meter.MarkFaulty("Fault reason");

        meter.Reactivate();

        meter.Status.Should().Be(MeterStatus.Active);
    }

    [Fact]
    public void Reactivate_Throws_When_Already_Active()
    {
        Meter meter = InstallMeter();

        Action act = () => meter.Reactivate();

        act.Should().Throw<MeterInvalidStateTransitionException>();
    }

    [Fact]
    public void Deactivate_From_Active_Sets_Inactive()
    {
        Meter meter = InstallMeter();

        meter.Deactivate();

        meter.Status.Should().Be(MeterStatus.Inactive);
    }

    [Fact]
    public void ReplaceWith_Marks_Old_Replaced_And_Returns_New_Meter_Pointing_Back()
    {
        Meter oldMeter = InstallMeter();

        Meter newMeter = oldMeter.ReplaceWith("MTR-002", Now.AddDays(1));

        oldMeter.Status.Should().Be(MeterStatus.Replaced);
        newMeter.Status.Should().Be(MeterStatus.Active);
        newMeter.MeterNumber.Should().Be("MTR-002");
        newMeter.ReplacesMeterId.Should().Be(oldMeter.Id);
        newMeter.TenantId.Should().Be(oldMeter.TenantId);
        newMeter.BuildingId.Should().Be(oldMeter.BuildingId);
        newMeter.UtilityType.Should().Be(oldMeter.UtilityType);
    }

    [Fact]
    public void ReplaceWith_Throws_When_Already_Replaced()
    {
        Meter meter = InstallMeter();
        meter.ReplaceWith("MTR-002", Now);

        Action act = () => meter.ReplaceWith("MTR-003", Now.AddDays(1));

        act.Should().Throw<MeterInvalidStateTransitionException>();
    }
}
