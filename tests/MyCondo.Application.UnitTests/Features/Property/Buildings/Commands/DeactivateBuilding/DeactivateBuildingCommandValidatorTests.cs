using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Buildings.Commands.DeactivateBuilding;

namespace MyCondo.Application.UnitTests.Features.Property.Buildings.Commands.DeactivateBuilding;

public class DeactivateBuildingCommandValidatorTests
{
    private readonly DeactivateBuildingCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DeactivateBuildingCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        DeactivateBuildingCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
