using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Flats.Commands.DeactivateFlat;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Commands.DeactivateFlat;

public class DeactivateFlatCommandValidatorTests
{
    private readonly DeactivateFlatCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DeactivateFlatCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FlatId_Fails()
    {
        DeactivateFlatCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
