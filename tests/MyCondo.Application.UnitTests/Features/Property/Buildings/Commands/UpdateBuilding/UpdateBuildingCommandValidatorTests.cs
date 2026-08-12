using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Buildings.Commands.UpdateBuilding;

namespace MyCondo.Application.UnitTests.Features.Property.Buildings.Commands.UpdateBuilding;

public class UpdateBuildingCommandValidatorTests
{
    private readonly UpdateBuildingCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        UpdateBuildingCommand command = new(Guid.NewGuid(), "ARP Tower", "ARP1", "123 Gulshan Ave");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        UpdateBuildingCommand command = new(Guid.Empty, "ARP Tower", "ARP1", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBuildingCommand.BuildingId));
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        UpdateBuildingCommand command = new(Guid.NewGuid(), "", "ARP1", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBuildingCommand.Name));
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        UpdateBuildingCommand command = new(Guid.NewGuid(), "ARP Tower", "", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBuildingCommand.Code));
    }

    [Fact]
    public void Null_Address_Is_Valid()
    {
        UpdateBuildingCommand command = new(Guid.NewGuid(), "ARP Tower", "ARP1", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
