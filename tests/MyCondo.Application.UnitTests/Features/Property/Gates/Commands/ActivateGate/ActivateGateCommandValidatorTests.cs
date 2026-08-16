using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Gates.Commands.ActivateGate;

namespace MyCondo.Application.UnitTests.Features.Property.Gates.Commands.ActivateGate;

public class ActivateGateCommandValidatorTests
{
    private readonly ActivateGateCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        ActivateGateCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_GateId_Fails()
    {
        ActivateGateCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
