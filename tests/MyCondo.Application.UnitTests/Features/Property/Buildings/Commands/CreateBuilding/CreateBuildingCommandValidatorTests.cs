using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;

namespace MyCondo.Application.UnitTests.Features.Property.Buildings.Commands.CreateBuilding;

public class CreateBuildingCommandValidatorTests
{
    private readonly CreateBuildingCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateBuildingCommand command = new("ARP Tower", "123 Gulshan Ave");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        CreateBuildingCommand command = new("", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBuildingCommand.Name));
    }

    [Fact]
    public void Name_Over_MaxLength_Fails()
    {
        CreateBuildingCommand command = new(new string('a', 201), null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBuildingCommand.Name));
    }

    [Fact]
    public void Null_Address_Is_Valid()
    {
        CreateBuildingCommand command = new("ARP Tower", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
