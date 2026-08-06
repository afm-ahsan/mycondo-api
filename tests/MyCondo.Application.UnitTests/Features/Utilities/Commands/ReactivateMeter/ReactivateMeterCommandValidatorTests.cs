using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.ReactivateMeter;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.ReactivateMeter;

public class ReactivateMeterCommandValidatorTests
{
    private readonly ReactivateMeterCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        ReactivateMeterCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MeterId_Fails()
    {
        ReactivateMeterCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReactivateMeterCommand.MeterId));
    }
}
