using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.CorrectReading;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.CorrectReading;

public class CorrectReadingCommandValidatorTests
{
    private readonly CorrectReadingCommandValidator _validator = new();
    private static readonly DateOnly ReadingDate = new(2026, 3, 31);

    [Fact]
    public void Valid_Command_Passes()
    {
        CorrectReadingCommand command = new(Guid.NewGuid(), 100m, 250m, ReadingDate, null, "Wrong meter reading transcribed");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ReadingId_Fails()
    {
        CorrectReadingCommand command = new(Guid.Empty, 100m, 250m, ReadingDate, null, "Correction reason");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CorrectReadingCommand.ReadingId));
    }

    [Fact]
    public void Blank_Reason_Fails()
    {
        CorrectReadingCommand command = new(Guid.NewGuid(), 100m, 250m, ReadingDate, null, "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CorrectReadingCommand.Reason));
    }
}
