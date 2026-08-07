using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.UnitTests.Features.Operations.Generators;

public class GeneratorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();

    private static Generator CreateGenerator() =>
        Generator.Create(TenantId, BuildingId, "Generator 1", "Cummins C150", 150m, "Roof", Now);

    [Fact]
    public void Create_Starts_Active_With_Zero_Hour_Meter()
    {
        Generator generator = CreateGenerator();

        generator.IsActive.Should().BeTrue();
        generator.Version.Should().Be(1);
        generator.CurrentHourMeterReading.Should().Be(0m);
    }

    [Fact]
    public void Create_Throws_When_CapacityKva_Negative()
    {
        Action act = () => Generator.Create(TenantId, BuildingId, "Generator 1", null, -1m, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_Updates_Fields_And_Bumps_Version()
    {
        Generator generator = CreateGenerator();

        generator.UpdateDetails("Generator 1 (Renamed)", "Cummins C200", 200m, "Basement");

        generator.Name.Should().Be("Generator 1 (Renamed)");
        generator.CapacityKva.Should().Be(200m);
        generator.Version.Should().Be(2);
    }

    [Fact]
    public void AdvanceHourMeter_Accepts_Higher_Reading()
    {
        Generator generator = CreateGenerator();

        generator.AdvanceHourMeter(12.5m);

        generator.CurrentHourMeterReading.Should().Be(12.5m);
        generator.Version.Should().Be(2);
    }

    [Fact]
    public void AdvanceHourMeter_Throws_When_Reading_Lower_Than_Current()
    {
        Generator generator = CreateGenerator();
        generator.AdvanceHourMeter(50m);

        Action act = () => generator.AdvanceHourMeter(40m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_Then_Reactivate_Toggles_IsActive()
    {
        Generator generator = CreateGenerator();

        generator.Deactivate();
        generator.IsActive.Should().BeFalse();

        generator.Reactivate();
        generator.IsActive.Should().BeTrue();
        generator.Version.Should().Be(3);
    }
}
