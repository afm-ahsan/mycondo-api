using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.BillReading;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.BillReading;

public class BillReadingCommandValidatorTests
{
    private readonly BillReadingCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        BillReadingCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ReadingId_Fails()
    {
        BillReadingCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BillReadingCommand.ReadingId));
    }
}
