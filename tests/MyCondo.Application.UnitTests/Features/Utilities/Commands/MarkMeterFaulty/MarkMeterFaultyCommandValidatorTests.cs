using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.MarkMeterFaulty;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.MarkMeterFaulty;

public class MarkMeterFaultyCommandValidatorTests
{
    private readonly MarkMeterFaultyCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        MarkMeterFaultyCommand command = new(Guid.NewGuid(), "Not registering readings");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MeterId_Fails()
    {
        MarkMeterFaultyCommand command = new(Guid.Empty, "Not registering readings");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(MarkMeterFaultyCommand.MeterId));
    }

    [Fact]
    public void Blank_Reason_Fails()
    {
        MarkMeterFaultyCommand command = new(Guid.NewGuid(), "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(MarkMeterFaultyCommand.Reason));
    }
}
