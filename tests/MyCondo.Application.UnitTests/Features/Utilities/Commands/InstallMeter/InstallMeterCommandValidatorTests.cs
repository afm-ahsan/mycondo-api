using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.InstallMeter;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.InstallMeter;

public class InstallMeterCommandValidatorTests
{
    private readonly InstallMeterCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        InstallMeterCommand command = new(Guid.NewGuid(), "Electricity", "MTR-001");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        InstallMeterCommand command = new(Guid.Empty, "Electricity", "MTR-001");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InstallMeterCommand.BuildingId));
    }

    [Fact]
    public void Invalid_UtilityType_Fails()
    {
        InstallMeterCommand command = new(Guid.NewGuid(), "Water", "MTR-001");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InstallMeterCommand.UtilityType));
    }

    [Fact]
    public void Empty_MeterNumber_Fails()
    {
        InstallMeterCommand command = new(Guid.NewGuid(), "Electricity", "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InstallMeterCommand.MeterNumber));
    }
}
