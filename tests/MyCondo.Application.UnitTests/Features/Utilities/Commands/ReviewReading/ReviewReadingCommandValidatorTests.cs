using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.ReviewReading;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.ReviewReading;

public class ReviewReadingCommandValidatorTests
{
    private readonly ReviewReadingCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        ReviewReadingCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ReadingId_Fails()
    {
        ReviewReadingCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReviewReadingCommand.ReadingId));
    }
}
