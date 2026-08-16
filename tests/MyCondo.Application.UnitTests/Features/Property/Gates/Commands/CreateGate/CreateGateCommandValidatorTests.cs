using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Gates.Commands.CreateGate;

namespace MyCondo.Application.UnitTests.Features.Property.Gates.Commands.CreateGate;

public class CreateGateCommandValidatorTests
{
    private readonly CreateGateCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateGateCommand command = new(Guid.NewGuid(), "Main Gate", "MAIN", null, true, true, 0);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        CreateGateCommand command = new(Guid.NewGuid(), "", "MAIN", null, true, true, 0);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGateCommand.Name));
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        CreateGateCommand command = new(Guid.NewGuid(), "Main Gate", "", null, true, true, 0);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGateCommand.Code));
    }
}
