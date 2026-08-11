using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Residents.Commands.DisableResident;

namespace MyCondo.Application.UnitTests.Features.Residents.Commands.DisableResident;

public class DisableResidentCommandValidatorTests
{
    private readonly DisableResidentCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DisableResidentCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ResidentId_Fails()
    {
        DisableResidentCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DisableResidentCommand.ResidentId));
    }
}
