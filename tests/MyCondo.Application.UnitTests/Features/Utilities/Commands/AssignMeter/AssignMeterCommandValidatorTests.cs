using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.AssignMeter;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.AssignMeter;

public class AssignMeterCommandValidatorTests
{
    private readonly AssignMeterCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        AssignMeterCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MeterId_Fails()
    {
        AssignMeterCommand command = new(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssignMeterCommand.MeterId));
    }

    [Fact]
    public void Empty_FlatId_Fails()
    {
        AssignMeterCommand command = new(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssignMeterCommand.FlatId));
    }
}
