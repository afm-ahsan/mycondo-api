using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Gates.Commands.DeactivateGate;

namespace MyCondo.Application.UnitTests.Features.Property.Gates.Commands.DeactivateGate;

public class DeactivateGateCommandValidatorTests
{
    private readonly DeactivateGateCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DeactivateGateCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_GateId_Fails()
    {
        DeactivateGateCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
