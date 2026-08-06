using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.FinalizeReading;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.FinalizeReading;

public class FinalizeReadingCommandValidatorTests
{
    private readonly FinalizeReadingCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        FinalizeReadingCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ReadingId_Fails()
    {
        FinalizeReadingCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(FinalizeReadingCommand.ReadingId));
    }
}
