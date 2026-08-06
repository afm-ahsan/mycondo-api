using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.ReplaceMeter;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.ReplaceMeter;

public class ReplaceMeterCommandValidatorTests
{
    private readonly ReplaceMeterCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        ReplaceMeterCommand command = new(Guid.NewGuid(), "MTR-002");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MeterId_Fails()
    {
        ReplaceMeterCommand command = new(Guid.Empty, "MTR-002");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReplaceMeterCommand.MeterId));
    }

    [Fact]
    public void Empty_NewMeterNumber_Fails()
    {
        ReplaceMeterCommand command = new(Guid.NewGuid(), "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReplaceMeterCommand.NewMeterNumber));
    }
}
