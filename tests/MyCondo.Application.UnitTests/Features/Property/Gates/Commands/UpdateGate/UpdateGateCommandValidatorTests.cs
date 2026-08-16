using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Gates.Commands.UpdateGate;

namespace MyCondo.Application.UnitTests.Features.Property.Gates.Commands.UpdateGate;

public class UpdateGateCommandValidatorTests
{
    private readonly UpdateGateCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        UpdateGateCommand command = new(Guid.NewGuid(), "Main Gate", "MAIN", null, true, true, 0);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_GateId_Fails()
    {
        UpdateGateCommand command = new(Guid.Empty, "Main Gate", "MAIN", null, true, true, 0);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateGateCommand.GateId));
    }
}
